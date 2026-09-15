using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Pricing;
using WMS.Application.DTOs.Products;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Qoralamadagi bitta qator — model yuboradigan shakl.</summary>
public sealed class DraftItemArgs
{
    /// <summary><c>find_product</c> qaytargan id.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Mahsulot nomi — id bo'lmaganda.</summary>
    public string? ProductName { get; set; }

    /// <summary>Miqdor ASOSIY birlikda (dona, kg, litr).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Qadoq soni («50 quti») — <see cref="Quantity"/> O'RNIGA.</summary>
    public decimal? Packs { get; set; }

    /// <summary>Birlik narxi; bo'sh — oxirgi narx (chiqim) yoki tannarx (kirim).</summary>
    public decimal? UnitPrice { get; set; }
}

/// <summary>Hujjat qoralamasi argumentlari.</summary>
public sealed class DraftTransferArgs
{
    /// <summary><c>incoming</c> — kirim, <c>outgoing</c> — chiqim.</summary>
    public TransferType Type { get; set; } = TransferType.Outgoing;

    /// <summary><c>find_counterparty</c> qaytargan id.</summary>
    public Guid? CounterpartyId { get; set; }

    /// <summary>Kontragent nomi — id bo'lmaganda.</summary>
    public string? CounterpartyName { get; set; }

    /// <summary>Ombor nomi; bo'sh — tenantning standart ombori (P2.6).</summary>
    public string? WarehouseName { get; set; }

    /// <summary>Hujjat sanasi (<c>yyyy-MM-dd</c>); bo'sh — bugun.</summary>
    public DateOnly? DocumentDate { get; set; }

    public string? Note { get; set; }

    public List<DraftItemArgs> Items { get; set; } = [];
}

/// <summary>
/// <c>draft_transfer</c> — kirim/chiqim hujjatining QORALAMASI.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Hujjat YARATILMAYDI</b> (F10 §0.6). Tool to'liq qoralama qaytaradi; yozuvni odam
/// tugma bosganda yuza yaratadi va u ham <c>Pending</c> bo'lib tushadi — tasdiqlash yana
/// bir alohida qadam. <c>confirm_transfer</c> degan tool YO'Q va bo'lmaydi.
/// </para>
/// <para>
/// ⚠️ Qoldiq yetmasa qoralama BARIBIR qaytariladi, faqat qatorda belgilanadi: «yetmaydi» —
/// foydalanuvchi ko'rishi kerak bo'lgan holat, qoralamani tashlab yuborish sababi emas.
/// Aks holda AI «qila olmadim» deb qo'ya qolardi va odam sababini bilmasdi.
/// </para>
/// </remarks>
public sealed class DraftTransferTool : AiTool<DraftTransferArgs>
{
    private readonly ISearchService _search;
    private readonly IProductService _products;
    private readonly IWarehouseService _warehouses;
    private readonly IStockAllocator _stock;
    private readonly IPricingService _pricing;

    public DraftTransferTool(
        ISearchService search,
        IProductService products,
        IWarehouseService warehouses,
        IStockAllocator stock,
        IPricingService pricing)
    {
        _search = search;
        _products = products;
        _warehouses = warehouses;
        _stock = stock;
        _pricing = pricing;
    }

    public override string Code => "draft_transfer";

    public override string Description =>
        "Kirim yoki chiqim hujjatining QORALAMASINI tayyorlaydi — hujjat YARATMAYDI. "
        + "Foydalanuvchi qoralamani ko'rib, o'zi tugma bosib yaratadi. Narx berilmasa "
        + "chiqimda oxirgi sotuv narxi, kirimda tannarx qo'yiladi. Miqdorni asosiy "
        + "birlikda (quantity) yoki qadoq soni bilan (packs) bering.";

    public override string PermissionCode => WmsPermissions.TransfersCreate;

    protected override async Task<AiToolResult> RunAsync(
        DraftTransferArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        if (args.Items.Count == 0)
        {
            return AiToolResult.Fail("Qatorlar yo'q: kamida bitta mahsulot va miqdor kerak.");
        }

        if (args.Type is not (TransferType.Incoming or TransferType.Outgoing))
        {
            return AiToolResult.Fail("Faqat kirim (incoming) va chiqim (outgoing) qoralamasi tayyorlanadi.");
        }

        // ⚠️ Feature tool darajasida EMAS, shu yerda: kirim va chiqim ALOHIDA sotiladi
        // (`TransferService.EnsureTransferTypeAllowedAsync` bilan bir xil qoida).
        string feature = args.Type == TransferType.Incoming
            ? FeatureCodes.TransfersIncoming
            : FeatureCodes.TransfersOutgoing;

        if (!context.HasFeature(feature))
        {
            return AiToolResult.Fail($"Bu imkoniyat tenantda yoqilmagan ({feature}).");
        }

        // ── Kontragent ──
        (Guid CounterpartyId, string Name)? counterparty = null;
        if (args.CounterpartyId is { } id)
        {
            counterparty = (id, "—");
        }
        else if (!string.IsNullOrWhiteSpace(args.CounterpartyName))
        {
            NameResolution<CounterpartySearchCandidate> resolved = AiNameResolver.Resolve(
                await _search.FindCounterpartiesAsync(args.CounterpartyName, AiNameResolver.MaxCandidates, cancellationToken));

            if (resolved.NotFound)
            {
                return new AiToolResult(AiNameResolver.NotFoundText(args.CounterpartyName, "kontragent"));
            }

            if (resolved.IsAmbiguous)
            {
                return new AiToolResult(AiNameResolver.AmbiguousText(
                    args.CounterpartyName, resolved.Candidates, c => $"{c.Name} ({c.Type})"));
            }

            counterparty = (resolved.Match!.Id, resolved.Match.Name);
        }
        else
        {
            return new AiToolResult("Kontragent ko'rsatilmagan — kimga/kimdan ekanini foydalanuvchidan so'rang.");
        }

        // ── Ombor ──
        object warehouse = await ResolveWarehouseAsync(args, cancellationToken);
        if (warehouse is AiToolResult warehouseProblem)
        {
            return warehouseProblem;
        }

        (Guid warehouseId, string warehouseName) = ((Guid, string))warehouse;

        // ── Qatorlar ──
        List<AiDraftItem> items = [];
        foreach (DraftItemArgs line in args.Items)
        {
            object resolved = await ResolveItemAsync(line, args, counterparty.Value.CounterpartyId, warehouseId, cancellationToken);
            if (resolved is AiToolResult problem)
            {
                return problem;
            }

            items.Add((AiDraftItem)resolved);
        }

        DateTime documentDate = args.DocumentDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.Date;

        AiTransferDraft draft = new(
            args.Type,
            counterparty.Value.CounterpartyId,
            counterparty.Value.Name,
            warehouseId,
            warehouseName,
            documentDate,
            items,
            args.Note);

        return new AiToolResult(Describe(draft), draft);
    }

    /// <summary>
    /// Omborni yechadi: nom berilsa qidiruvdan, aks holda tenant sukutidan (P2.6).
    /// </summary>
    /// <returns><c>(Guid, string)</c> yoki muammo bo'lsa <see cref="AiToolResult"/>.</returns>
    /// <remarks>
    /// <para>
    /// ⚠️ Sukut ombor YO'Q va bittadan ko'p bo'lsa — SAVOL, birinchisini olish emas:
    /// noto'g'ri ombordan chiqim boshqa omborning qoldig'ini buzardi.
    /// </para>
    /// <para>
    /// ⚠️ Natija QAYTARILADI, maydonga yozilmaydi: model bitta aylanishda `draft_transfer`
    /// ni ikki marta chaqirishi mumkin (parallel tool chaqiriqlari) va umumiy maydon
    /// ikkinchi qoralamani birinchisining ombori bilan yozib ketardi.
    /// </para>
    /// </remarks>
    private async Task<object> ResolveWarehouseAsync(DraftTransferArgs args, CancellationToken cancellationToken)
    {
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

            return (resolved.Match!.Id, resolved.Match.Name);
        }

        WarehouseDefaultsDto defaults = await _warehouses.GetDefaultsAsync(cancellationToken);

        // Kirim — tayyor mahsulot ombori, chiqim ham o'sha: tenantda ikkisi bir xil
        // bo'lishi mumkin. Aniqlanmasa savol beriladi.
        Guid? effective = args.Type == TransferType.Incoming
            ? defaults.EffectiveFinishedId ?? defaults.EffectiveRawId
            : defaults.EffectiveFinishedId ?? defaults.EffectiveRawId;

        if (effective is null)
        {
            return new AiToolResult(
                "Ombor aniqlanmadi: tenantda bir nechta ombor bor va standart ombor tanlanmagan. "
                + "Qaysi ombor kerakligini foydalanuvchidan so'rang.");
        }

        List<WarehouseDto> all = await _warehouses.GetAllAsync();
        string name = all.FirstOrDefault(w => w.Id == effective.Value)?.Name ?? "—";

        return (effective.Value, name);
    }

    /// <summary>Bitta qatorni yechadi: mahsulot, miqdor, narx, mavjudlik.</summary>
    /// <returns><see cref="AiDraftItem"/> yoki muammo bo'lsa <see cref="AiToolResult"/>.</returns>
    private async Task<object> ResolveItemAsync(
        DraftItemArgs line,
        DraftTransferArgs args,
        Guid counterpartyId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        Guid productId;

        if (line.ProductId is { } id)
        {
            productId = id;
        }
        else if (!string.IsNullOrWhiteSpace(line.ProductName))
        {
            NameResolution<ProductSearchCandidate> resolved = AiNameResolver.Resolve(
                await _search.FindProductsAsync(line.ProductName, AiNameResolver.MaxCandidates, cancellationToken));

            if (resolved.NotFound)
            {
                return new AiToolResult(AiNameResolver.NotFoundText(line.ProductName, "mahsulot"));
            }

            if (resolved.IsAmbiguous)
            {
                return new AiToolResult(AiNameResolver.AmbiguousText(line.ProductName, resolved.Candidates, c => c.Name));
            }

            productId = resolved.Match!.Id;
        }
        else
        {
            return AiToolResult.Fail("Qatorda mahsulot ko'rsatilmagan.");
        }

        ProductDto product = await _products.GetByIdAsync(productId);

        // ── Miqdor: qadoq → asosiy birlik ──
        decimal quantity;
        if (line.Packs is { } packs and > 0)
        {
            if (product.PackSize is not { } packSize || packSize <= 0)
            {
                return new AiToolResult(
                    $"'{product.Name}' uchun qadoq o'lchami kiritilmagan — miqdorni asosiy birlikda so'rang.");
            }

            quantity = packs * packSize;
        }
        else
        {
            quantity = line.Quantity;
        }

        if (quantity <= 0)
        {
            return AiToolResult.Fail($"'{product.Name}' miqdori noldan katta bo'lishi kerak.");
        }

        // ── Narx ──
        decimal price;
        string priceSource;

        if (line.UnitPrice is { } given && given >= 0)
        {
            price = given;
            priceSource = "user";
        }
        else if (args.Type == TransferType.Outgoing)
        {
            LastPriceDto? last = await _pricing.GetLastPriceAsync(
                productId, counterpartyId, TransferType.Outgoing, cancellationToken);

            price = last?.UnitPrice ?? 0m;
            priceSource = last is null ? "none" : "last_price";
        }
        else
        {
            price = product.CostPrice ?? 0m;
            priceSource = product.CostPrice is null ? "none" : "cost";
        }

        // ── Mavjudlik (faqat chiqimda) ──
        decimal? available = null;
        if (args.Type == TransferType.Outgoing)
        {
            Dictionary<Guid, decimal> stock = await _stock.GetAvailableAsync([productId], warehouseId, cancellationToken);
            available = stock.GetValueOrDefault(productId);
        }

        return new AiDraftItem(productId, product.Name, product.UnitShortName, quantity, price, priceSource, available);
    }

    /// <summary>
    /// Modelga ketadigan matn.
    /// </summary>
    /// <remarks>
    /// ⚠️ Narx QAYERDAN olingani aytiladi: model «12 000» ni o'zi topib qo'ygandek
    /// gapirmasin — foydalanuvchi taklif qilingan narxni tekshirishi kerak.
    /// </remarks>
    private static string Describe(AiTransferDraft draft)
    {
        string kind = draft.Type == TransferType.Incoming ? "KIRIM" : "CHIQIM";

        List<string> lines =
        [
            $"{kind} qoralamasi (hujjat HALI yaratilmadi):",
            $"- Kontragent: {draft.CounterpartyName}",
            $"- Ombor: {draft.WarehouseName}",
            $"- Sana: {Date(draft.DocumentDate)}",
        ];

        foreach (AiDraftItem item in draft.Items)
        {
            string price = item.PriceSource switch
            {
                "last_price" => $"{Money(item.UnitPrice)} (oxirgi narx)",
                "cost" => $"{Money(item.UnitPrice)} (tannarx)",
                "none" => "narx TOPILMADI — foydalanuvchidan so'rang",
                _ => Money(item.UnitPrice),
            };

            string shortfall = item.Shortfall
                ? $" ⚠️ mavjud atigi {Quantity(item.Available ?? 0)} — YETMAYDI"
                : string.Empty;

            lines.Add($"- {item.ProductName}: {Quantity(item.Quantity)} {item.UnitShortName} × {price}{shortfall}");
        }

        lines.Add($"- Jami: {Money(draft.Total)}");
        lines.Add("Foydalanuvchiga qoralamani ko'rsating va tasdiqlashni SO'RANG — hujjatni o'zingiz yarata olmaysiz.");

        return string.Join("\n", lines);
    }
}
