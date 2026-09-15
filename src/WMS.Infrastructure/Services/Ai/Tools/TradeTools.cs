using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Analytics;
using WMS.Application.DTOs.Pricing;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Kutilayotgan hujjatlar argumentlari.</summary>
public sealed class PendingTransfersArgs
{
    /// <summary>Tur bo'yicha filtr: <c>incoming</c>, <c>outgoing</c>, <c>internal</c>, <c>return</c>.</summary>
    public TransferType? Type { get; set; }
}

/// <summary>
/// <c>pending_transfers</c> — tasdiq kutayotgan hujjatlar.
/// </summary>
/// <remarks>
/// ⚠️ Faqat <see cref="TransferStatus.Pending"/>: «kutilayotgan» degani aynan shu.
/// Tasdiqlangan hujjatlar ro'yxati boshqa savol va uni bu yerga qo'shish javobni
/// ma'nosiz uzaytirardi.
/// </remarks>
public sealed class PendingTransfersTool : AiTool<PendingTransfersArgs>
{
    private const int PageSize = 100;

    private readonly ITransferService _transfers;

    public PendingTransfersTool(ITransferService transfers) => _transfers = transfers;

    public override string Code => "pending_transfers";

    public override string Description =>
        "Tasdiq kutayotgan kirim/chiqim hujjatlarini qaytaradi. type bilan turini "
        + "cheklash mumkin (incoming, outgoing, internal, return).";

    public override string PermissionCode => WmsPermissions.TransfersView;

    protected override async Task<AiToolResult> RunAsync(
        PendingTransfersArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        List<TransferDto> transfers = await _transfers.GetAllAsync(
            args.Type, TransferStatus.Pending, page: 1, pageSize: PageSize);

        if (transfers.Count == 0)
        {
            return new AiToolResult("Tasdiq kutayotgan hujjat yo'q.");
        }

        IReadOnlyList<TransferDto> shown = Limit(transfers, out bool truncated);

        string text = string.Join("\n", shown.Select(t =>
        {
            string where = t.CounterpartyName ?? t.FromWarehouseName ?? t.ToWarehouseName ?? "—";
            return $"- #{t.Number} {t.Type}, {Date(t.DocumentDate)}, {where}";
        }));

        string header = truncated
            ? $"{transfers.Count} ta hujjat kutmoqda, birinchi {shown.Count} tasi:"
            : $"{shown.Count} ta hujjat tasdiq kutmoqda:";

        return new AiToolResult($"{header}\n{text}", shown);
    }
}

/// <summary>Oxirgi narx argumentlari.</summary>
public sealed class LastPriceArgs
{
    /// <summary><c>find_product</c> qaytargan id.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Mahsulot nomi — id bo'lmaganda.</summary>
    public string? ProductName { get; set; }

    /// <summary>Kontragent id — u bilan kelishilgan narx umumiydan ustun.</summary>
    public Guid? CounterpartyId { get; set; }

    /// <summary>
    /// <c>outgoing</c> — SOTUV narxi (sukut), <c>incoming</c> — KIRIM narxi.
    /// </summary>
    public TransferType Type { get; set; } = TransferType.Outgoing;
}

/// <summary>
/// <c>last_price</c> — oxirgi tasdiqlangan hujjatdagi narx.
/// </summary>
/// <remarks>
/// ⚠️ Sotuv va kirim narxi ARALASHMAYDI (<see cref="IPricingService"/> qoidasi): sotuv
/// narxini kirimga taklif qilish tannarxni sotuv darajasiga ko'tarib, foydani nolga
/// tushirardi. Shuning uchun <c>type</c> tool argumenti va sukuti — sotuv.
/// </remarks>
public sealed class LastPriceTool : AiTool<LastPriceArgs>
{
    private readonly IPricingService _pricing;
    private readonly ISearchService _search;

    public LastPriceTool(IPricingService pricing, ISearchService search)
    {
        _pricing = pricing;
        _search = search;
    }

    public override string Code => "last_price";

    public override string Description =>
        "Mahsulotning oxirgi tasdiqlangan hujjatdagi narxini qaytaradi. type=outgoing — "
        + "sotuv narxi (sukut), type=incoming — kirim narxi. counterpartyId berilsa avval "
        + "AYNAN shu kontragent bilan bo'lgan narx qaraladi.";

    public override string PermissionCode => WmsPermissions.TransfersView;

    protected override async Task<AiToolResult> RunAsync(
        LastPriceArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        Guid productId;
        string? productName = null;

        if (args.ProductId is { } id)
        {
            productId = id;
        }
        else if (!string.IsNullOrWhiteSpace(args.ProductName))
        {
            NameResolution<ProductSearchCandidate> resolved = AiNameResolver.Resolve(
                await _search.FindProductsAsync(args.ProductName, AiNameResolver.MaxCandidates, cancellationToken));

            if (resolved.NotFound)
            {
                return new AiToolResult(AiNameResolver.NotFoundText(args.ProductName, "mahsulot"));
            }

            if (resolved.IsAmbiguous)
            {
                return new AiToolResult(AiNameResolver.AmbiguousText(args.ProductName, resolved.Candidates, c => c.Name));
            }

            productId = resolved.Match!.Id;
            productName = resolved.Match.Name;
        }
        else
        {
            return AiToolResult.Fail("Mahsulot ko'rsatilmadi: productId yoki productName bering.");
        }

        LastPriceDto? price = await _pricing.GetLastPriceAsync(productId, args.CounterpartyId, args.Type, cancellationToken);

        if (price is null)
        {
            // ⚠️ Nol narx TAKLIF EMAS (servis shartnomasi): «narx 0» deb javob berish
            // qoralamaga nol narx tushirib, hujjatni jimgina yaroqsiz qilardi.
            string kind = args.Type == TransferType.Incoming ? "kirim" : "sotuv";
            return new AiToolResult($"{productName ?? productId.ToString()} uchun oldingi {kind} narxi topilmadi.");
        }

        string source = price.IsSameCounterparty
            ? $"shu kontragent bilan (#{price.Number}, {Date(price.DocumentDate)})"
            : $"umumiy oxirgi narx (#{price.Number}, {Date(price.DocumentDate)}"
              + (price.CounterpartyName is { Length: > 0 } name ? $", {name})" : ")");

        return new AiToolResult(
            $"{productName ?? "Mahsulot"}: oxirgi narx {Money(price.UnitPrice)} — {source}.",
            price);
    }
}

/// <summary>
/// <c>today_summary</c> — bugungi umumiy manzara.
/// </summary>
public sealed class TodaySummaryTool : AiTool<NoArgs>
{
    private readonly IAnalyticsService _analytics;

    public TodaySummaryTool(IAnalyticsService analytics) => _analytics = analytics;

    public override string Code => "today_summary";

    public override string Description =>
        "Bugungi umumiy manzara: umumiy qoldiq, kutilayotgan hujjatlar, oylik tushum, "
        + "qarz va kam qolgan mahsulotlar soni.";

    public override string PermissionCode => WmsPermissions.DashboardView;

    protected override async Task<AiToolResult> RunAsync(
        NoArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        DashboardSummaryDto summary = await _analytics.GetDashboardSummary();

        string text = string.Join("\n",
            $"- Umumiy qoldiq: {Quantity(summary.TotalStockKg)}",
            $"- Tasdiq kutayotgan hujjatlar: {summary.PendingTransfers}",
            $"- Faol ishlab chiqarish buyurtmalari: {summary.ActiveProductionOrders}",
            $"- Oylik tushum: {Money(summary.MonthlyRevenue)} ({summary.RevenueChangePercent:+0.#;-0.#;0} %)",
            $"- Umumiy qarz balansi: {Money(summary.TotalDebt)}",
            $"- Kam qolgan mahsulotlar: {summary.LowStockProductCount}");

        return new AiToolResult($"Bugungi holat:\n{text}", summary);
    }
}
