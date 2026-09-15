using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Analytics;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Qoldiq so'rovi argumentlari.</summary>
public sealed class StockQueryArgs
{
    /// <summary><c>find_product</c> qaytargan id. Ma'lum bo'lsa SHU ishlatiladi.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Mahsulot nomi — <see cref="ProductId"/> bo'lmaganda.</summary>
    public string? ProductName { get; set; }

    /// <summary>Ombor nomi; bo'sh — hamma ombor.</summary>
    public string? WarehouseName { get; set; }

    /// <summary>Faqat minimal darajadan pastga tushganlari.</summary>
    public bool OnlyLow { get; set; }
}

/// <summary>
/// <c>stock_query</c> — qoldiq: bitta mahsulot yoki butun ombor kesimi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Qaytariladigan son — MAVJUD miqdor (zaxiraga olingani ayrilgan), umumiy qoldiq emas.
/// Sabab: «sota olamizmi?» degan savolga javob beradigan son aynan shu. Umumiy qoldiqni
/// aytish tasdiqlanmagan chiqimlarga band qilingan tovarni ikkinchi marta sotishga olib
/// borardi. Servis ham (<see cref="IStockAllocator.GetAvailableAsync"/>) FEFO yechish bilan
/// BIR XIL shartdan quriladi — ya'ni AI aytgan son hujjat yaratishda ham o'zgarmaydi.
/// </para>
/// <para>
/// Mahsulot ko'rsatilmasa butun ombor kesimi beriladi — «nimalar tugab qolyapti?» savoli
/// uchun (<c>onlyLow</c>).
/// </para>
/// </remarks>
public sealed class StockQueryTool : AiTool<StockQueryArgs>
{
    private readonly IStockAllocator _stock;
    private readonly IAnalyticsService _analytics;
    private readonly ISearchService _search;

    public StockQueryTool(IStockAllocator stock, IAnalyticsService analytics, ISearchService search)
    {
        _stock = stock;
        _analytics = analytics;
        _search = search;
    }

    public override string Code => "stock_query";

    public override string Description =>
        "Ombordagi MAVJUD qoldiqni qaytaradi (zaxiraga olingani ayrilgan). Bitta mahsulot "
        + "uchun productId yoki productName bering; ikkalasi ham bo'lmasa butun ombor kesimi "
        + "qaytadi. onlyLow=true — faqat minimal darajadan pastga tushganlari.";

    public override string PermissionCode => WmsPermissions.WarehouseView;

    protected override async Task<AiToolResult> RunAsync(
        StockQueryArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        Guid? warehouseId = null;
        if (!string.IsNullOrWhiteSpace(args.WarehouseName))
        {
            NameResolution<WarehouseSearchCandidate> resolved = AiNameResolver.Resolve(
                await _search.FindWarehousesAsync(args.WarehouseName, AiNameResolver.MaxCandidates, cancellationToken));

            if (resolved.NotFound)
            {
                return new AiToolResult(AiNameResolver.NotFoundText(args.WarehouseName, "ombor"));
            }

            if (resolved.IsAmbiguous)
            {
                return new AiToolResult(AiNameResolver.AmbiguousText(
                    args.WarehouseName, resolved.Candidates, c => $"{c.Name} ({c.Type})"));
            }

            warehouseId = resolved.Match!.Id;
        }

        return args switch
        {
            { ProductId: { } id } => await SingleProductAsync(id, null, warehouseId, cancellationToken),
            { ProductName: { Length: > 0 } name } => await ByNameAsync(name, warehouseId, cancellationToken),
            _ => await OverviewAsync(warehouseId, args.OnlyLow, cancellationToken),
        };
    }

    private async Task<AiToolResult> ByNameAsync(string name, Guid? warehouseId, CancellationToken cancellationToken)
    {
        NameResolution<ProductSearchCandidate> resolved = AiNameResolver.Resolve(
            await _search.FindProductsAsync(name, AiNameResolver.MaxCandidates, cancellationToken));

        if (resolved.NotFound)
        {
            return new AiToolResult(AiNameResolver.NotFoundText(name, "mahsulot"));
        }

        if (resolved.IsAmbiguous)
        {
            return new AiToolResult(AiNameResolver.AmbiguousText(name, resolved.Candidates, c => c.Name));
        }

        return await SingleProductAsync(resolved.Match!.Id, resolved.Match.Name, warehouseId, cancellationToken);
    }

    private async Task<AiToolResult> SingleProductAsync(
        Guid productId, string? productName, Guid? warehouseId, CancellationToken cancellationToken)
    {
        Dictionary<Guid, decimal> available = await _stock.GetAvailableAsync([productId], warehouseId, cancellationToken);

        // ⚠️ Qatori yo'q mahsulot lug'atda ham YO'Q (servis shartnomasi) — 0 bilan
        // aralashtirilmaydi: «qoldiq nol» ham, «bu omborda umuman yo'q» ham 0 deb
        // ko'rinardi, lekin foydalanuvchi uchun bu boshqa-boshqa javob.
        decimal quantity = available.GetValueOrDefault(productId);
        string where = warehouseId is null ? "hamma omborda" : "shu omborda";
        string label = productName ?? productId.ToString();

        return new AiToolResult(
            $"{label}: {where} mavjud {Quantity(quantity)}.",
            new { productId, productName, warehouseId, availableQuantity = quantity });
    }

    private async Task<AiToolResult> OverviewAsync(Guid? warehouseId, bool onlyLow, CancellationToken cancellationToken)
    {
        List<StockLevelDto> levels = await _analytics.GetStockLevels(warehouseId);
        if (onlyLow)
        {
            levels = [.. levels.Where(l => l.IsLow)];
        }

        if (levels.Count == 0)
        {
            return new AiToolResult(onlyLow ? "Minimal darajadan past mahsulot yo'q." : "Qoldiq topilmadi.");
        }

        IReadOnlyList<StockLevelDto> rows = Limit(levels, out bool truncated);
        string text = string.Join("\n", rows.Select(l =>
            $"- {l.ProductName}: {Quantity(l.CurrentStock)} {l.UnitShortName}"
            + (l.IsLow ? $" (min {Quantity(l.MinStock)} — PAST)" : string.Empty)));

        string header = truncated
            ? $"{levels.Count} ta mahsulot topildi, birinchi {rows.Count} tasi ko'rsatilmoqda — so'rovni toraytiring:"
            : $"{rows.Count} ta mahsulot:";

        return new AiToolResult($"{header}\n{text}", rows);
    }
}

/// <summary>Muddat so'rovi argumentlari.</summary>
public sealed class ExpiryQueryArgs
{
    /// <summary>Necha kun ichida muddati tugaydiganlari (sukut 30).</summary>
    public int? Days { get; set; }

    /// <summary>Muddati ALLAQACHON o'tganlarini ham qo'shish.</summary>
    public bool IncludeExpired { get; set; } = true;
}

/// <summary>
/// <c>expiry_query</c> — muddati yaqinlashgan partiyalar.
/// </summary>
/// <remarks>
/// ⚠️ Qoldig'i tugagan partiyalar CHIQMAYDI: ular allaqachon sotilgan va ro'yxatda
/// turishining ma'nosi yo'q — «10 partiya muddati tugayapti» degan javob odamni
/// tekshirishga yuborib, hech narsa topmasligiga olib borardi.
/// </remarks>
public sealed class ExpiryQueryTool : AiTool<ExpiryQueryArgs>
{
    /// <summary>Sukut oyna — bir oy.</summary>
    private const int DefaultDays = 30;

    private readonly IWarehouseService _warehouses;

    public ExpiryQueryTool(IWarehouseService warehouses) => _warehouses = warehouses;

    public override string Code => "expiry_query";

    public override string Description =>
        "Muddati yaqin kunlarda tugaydigan partiyalarni qaytaradi (qoldig'i borlari). "
        + "days — necha kun ichida (sukut 30).";

    public override string PermissionCode => WmsPermissions.WarehouseView;

    /// <summary>Partiyalar alohida sotiladigan imkoniyat — tenantda o'chiq bo'lishi mumkin.</summary>
    public override string? FeatureCode => FeatureCodes.WarehouseBatches;

    protected override async Task<AiToolResult> RunAsync(
        ExpiryQueryArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        int days = args.Days is > 0 ? args.Days.Value : DefaultDays;
        DateTime limit = DateTime.UtcNow.Date.AddDays(days);

        List<BatchDto> batches = await _warehouses.GetBatchesAsync();

        var rows = batches
            .Where(b => b.ExpiryDate is not null && b.RemainingQuantity > 0)
            .Where(b => b.ExpiryDate!.Value.Date <= limit)
            .Where(b => args.IncludeExpired || b.ExpiryDate!.Value.Date >= DateTime.UtcNow.Date)
            .OrderBy(b => b.ExpiryDate)
            .ToList();

        if (rows.Count == 0)
        {
            return new AiToolResult($"{days} kun ichida muddati tugaydigan partiya yo'q.");
        }

        IReadOnlyList<BatchDto> shown = Limit(rows, out bool truncated);
        DateTime today = DateTime.UtcNow.Date;

        string text = string.Join("\n", shown.Select(b =>
        {
            int left = (b.ExpiryDate!.Value.Date - today).Days;
            string when = left < 0 ? $"{-left} kun OLDIN tugagan" : left == 0 ? "BUGUN tugaydi" : $"{left} kun qoldi";
            return $"- {b.ProductName}, partiya {b.LotNumber}: {Quantity(b.RemainingQuantity)} — {Date(b.ExpiryDate.Value)} ({when})";
        }));

        string header = truncated
            ? $"{rows.Count} ta partiya topildi, birinchi {shown.Count} tasi:"
            : $"{shown.Count} ta partiya:";

        return new AiToolResult($"{header}\n{text}", shown);
    }
}
