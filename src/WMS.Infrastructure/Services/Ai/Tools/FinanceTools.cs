using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Qarz so'rovi argumentlari.</summary>
public sealed class DebtQueryArgs
{
    /// <summary><c>find_counterparty</c> qaytargan id — bitta kontragent kesimi.</summary>
    public Guid? CounterpartyId { get; set; }

    /// <summary>Kontragent nomi — id bo'lmaganda.</summary>
    public string? CounterpartyName { get; set; }

    /// <summary>
    /// Kim kimga qarzdor: <c>owesUs</c> — bizga qarzdorlar, <c>weOwe</c> — biz qarzdormiz,
    /// <c>all</c> — ikkalasi (sukut).
    /// </summary>
    public DebtSide Side { get; set; } = DebtSide.All;
}

/// <summary>Qarz tomoni.</summary>
public enum DebtSide
{
    /// <summary>Ikkala tomon.</summary>
    All = 0,

    /// <summary>Bizga qarzdorlar (musbat balans).</summary>
    OwesUs = 1,

    /// <summary>Biz qarzdormiz (manfiy balans).</summary>
    WeOwe = 2,
}

/// <summary>
/// <c>debt_query</c> — kontragentlar bo'yicha qarz balansi.
/// </summary>
/// <remarks>
/// ⚠️ Balans ISHORASI ma'noni butunlay o'zgartiradi: musbat — kontragent BIZGA qarzdor,
/// manfiy — BIZ unga. Matnda shu ataylab so'z bilan yoziladi, ishora bilan emas: model
/// «-500 000» ni «qarzdor» deb o'qib, javobni teskarisiga aylantirib yuborardi.
/// </remarks>
public sealed class DebtQueryTool : AiTool<DebtQueryArgs>
{
    private readonly IFinanceService _finance;
    private readonly ISearchService _search;

    public DebtQueryTool(IFinanceService finance, ISearchService search)
    {
        _finance = finance;
        _search = search;
    }

    public override string Code => "debt_query";

    public override string Description =>
        "Kontragentlar bo'yicha qarz balansini qaytaradi. Bitta kontragent uchun "
        + "counterpartyId yoki counterpartyName bering. side: owesUs — bizga qarzdorlar, "
        + "weOwe — biz qarzdormiz, all — hammasi.";

    public override string PermissionCode => WmsPermissions.FinanceView;

    public override string? FeatureCode => FeatureCodes.FinanceDebts;

    protected override async Task<AiToolResult> RunAsync(
        DebtQueryArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        Guid? counterpartyId = args.CounterpartyId;

        if (counterpartyId is null && !string.IsNullOrWhiteSpace(args.CounterpartyName))
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

            counterpartyId = resolved.Match!.Id;
        }

        List<DebtDto> debts = await _finance.GetDebtsAsync();

        if (counterpartyId is { } id)
        {
            debts = [.. debts.Where(d => d.CounterpartyId == id)];
        }

        // Nol balans qarz emas — ro'yxatni behuda uzaytiradi.
        debts = [.. debts.Where(d => d.Amount != 0)];

        debts = args.Side switch
        {
            DebtSide.OwesUs => [.. debts.Where(d => d.Amount > 0)],
            DebtSide.WeOwe => [.. debts.Where(d => d.Amount < 0)],
            _ => debts,
        };

        if (debts.Count == 0)
        {
            return new AiToolResult("Qarz topilmadi (balans nol).");
        }

        debts = [.. debts.OrderByDescending(d => Math.Abs(d.Amount))];
        IReadOnlyList<DebtDto> shown = Limit(debts, out bool truncated);

        string text = string.Join("\n", shown.Select(d => d.Amount > 0
            ? $"- {d.CounterpartyName} BIZGA qarzdor: {Money(d.Amount)}"
            : $"- BIZ {d.CounterpartyName} ga qarzdormiz: {Money(-d.Amount)}"));

        string header = truncated
            ? $"{debts.Count} ta kontragent, birinchi {shown.Count} tasi:"
            : $"{shown.Count} ta kontragent:";

        return new AiToolResult($"{header}\n{text}", shown);
    }
}

/// <summary>To'lovlar tarixi argumentlari.</summary>
public sealed class PaymentHistoryArgs
{
    /// <summary>Kontragent id — bitta kontragent kesimi.</summary>
    public Guid? CounterpartyId { get; set; }

    /// <summary>Oxirgi necha kun (sukut 30).</summary>
    public int? Days { get; set; }
}

/// <summary>
/// <c>payment_history</c> — to'lovlar tarixi.
/// </summary>
/// <remarks>
/// Storno yozuvlari ham ko'rsatiladi va OSHKORA belgilanadi: qaytarilgan to'lov tarixda
/// qoladi (P2.9), uni ro'yxatdan yashirish «pul kelgan edi» degan noto'g'ri manzara berardi.
/// </remarks>
public sealed class PaymentHistoryTool : AiTool<PaymentHistoryArgs>
{
    private const int DefaultDays = 30;
    private const int PageSize = 100;

    private readonly IFinanceService _finance;

    public PaymentHistoryTool(IFinanceService finance) => _finance = finance;

    public override string Code => "payment_history";

    public override string Description =>
        "To'lovlar tarixini qaytaradi. counterpartyId — bitta kontragent bo'yicha; "
        + "days — oxirgi necha kun (sukut 30).";

    public override string PermissionCode => WmsPermissions.FinanceView;

    public override string? FeatureCode => FeatureCodes.FinancePayments;

    protected override async Task<AiToolResult> RunAsync(
        PaymentHistoryArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        int days = args.Days is > 0 ? args.Days.Value : DefaultDays;
        DateTime from = DateTime.UtcNow.Date.AddDays(-days);

        List<PaymentHistoryDto> payments = await _finance.GetPaymentsAsync(args.CounterpartyId, 1, PageSize);

        // Sana bo'yicha filtr — `DocumentDate` (P2.3): «kecha to'lagan» yozuv bugun
        // kiritilgan bo'lsa ham KECHAGI kunda turishi kerak.
        payments = [.. payments.Where(p => p.DocumentDate >= from).OrderByDescending(p => p.DocumentDate)];

        if (payments.Count == 0)
        {
            return new AiToolResult($"Oxirgi {days} kunda to'lov yo'q.");
        }

        IReadOnlyList<PaymentHistoryDto> shown = Limit(payments, out bool truncated);

        string text = string.Join("\n", shown.Select(p =>
        {
            string direction = p.Direction == PaymentDirection.In ? "bizga tushdi" : "biz to'ladik";
            string storno = p.ReversalOfId is not null ? " [QAYTARISH]" : string.Empty;
            return $"- {Date(p.DocumentDate)} {p.CounterpartyName}: {Money(p.Amount)} ({direction}){storno}";
        }));

        string header = truncated
            ? $"Oxirgi {days} kunda {payments.Count} ta to'lov, birinchi {shown.Count} tasi:"
            : $"Oxirgi {days} kunda {shown.Count} ta to'lov:";

        return new AiToolResult($"{header}\n{text}", shown);
    }
}

/// <summary>
/// <c>finance_summary</c> — kirim, chiqim, qarz va sof foyda.
/// </summary>
public sealed class FinanceSummaryTool : AiTool<NoArgs>
{
    private readonly IFinanceService _finance;

    public FinanceSummaryTool(IFinanceService finance) => _finance = finance;

    public override string Code => "finance_summary";

    public override string Description =>
        "Moliyaviy yakun: umumiy kirim, chiqim, qarz va sof foyda.";

    public override string PermissionCode => WmsPermissions.FinanceView;

    protected override async Task<AiToolResult> RunAsync(
        NoArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        FinanceSummaryDto summary = await _finance.GetSummaryAsync();

        string text = string.Join("\n",
            $"- Kirim: {Money(summary.TotalIncome)}",
            $"- Chiqim: {Money(summary.TotalExpense)}",
            $"- Sof foyda: {Money(summary.NetProfit)}",
            $"- Umumiy qarz balansi: {Money(summary.TotalDebt)}");

        return new AiToolResult($"Moliyaviy yakun:\n{text}", summary);
    }
}
