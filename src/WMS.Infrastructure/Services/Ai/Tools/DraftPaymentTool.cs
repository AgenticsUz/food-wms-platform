using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>To'lov qoralamasi argumentlari.</summary>
public sealed class DraftPaymentArgs
{
    /// <summary><c>find_counterparty</c> qaytargan id.</summary>
    public Guid? CounterpartyId { get; set; }

    /// <summary>Kontragent nomi — id bo'lmaganda.</summary>
    public string? CounterpartyName { get; set; }

    /// <summary>Summa (musbat).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// <c>in</c> — pul BIZGA tushdi (qarz kamayadi), <c>out</c> — biz to'ladik.
    /// </summary>
    /// <remarks>
    /// ⚠️ MAJBURIY. Gapdan yo'nalish aniq bo'lmasa tool rad javobi beradi va model
    /// foydalanuvchidan so'raydi — sababi tool izohida.
    /// </remarks>
    public PaymentDirection? Direction { get; set; }

    /// <summary>To'lov usuli; bo'sh — naqd.</summary>
    public PaymentMethod? Method { get; set; }

    /// <summary>To'lov sanasi (<c>yyyy-MM-dd</c>); bo'sh — bugun.</summary>
    public DateOnly? DocumentDate { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// <c>draft_payment</c> — to'lov yozuvining QORALAMASI.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Yo'nalish HECH QACHON taxmin qilinmaydi.</b> <c>CreatePaymentDto.Direction</c>
/// ixtiyoriy va bo'sh bo'lsa servis uni QARZ BELGISIDAN chiqaradi — odam uchun qulay,
/// AI uchun xavfli: balans nolga yaqin yoki teskari bo'lganda summa noto'g'ri tomonga
/// yozilib, xatoni ikki barobar qilardi. Shuning uchun bu yerda u MAJBURIY.
/// </para>
/// <para>
/// ⚠️ <b>Yozuv YARATILMAYDI</b> (§0.6): tool qoralama va balans oldi/keyin qaytaradi,
/// yozuvni odam tugma bosganda yuza yozadi. <c>confirm_payment</c> degan tool YO'Q.
/// </para>
/// <para>
/// ⚠️ Bu — HISOBOT yozuvi, bank amali emas: tizimda haqiqiy tranzaksiya yo'q. Shuning
/// uchun matnda «pul o'tdi» emas, «yozuv kiritiladi» deyiladi.
/// </para>
/// </remarks>
public sealed class DraftPaymentTool : AiTool<DraftPaymentArgs>
{
    private readonly ISearchService _search;
    private readonly IFinanceService _finance;

    public DraftPaymentTool(ISearchService search, IFinanceService finance)
    {
        _search = search;
        _finance = finance;
    }

    public override string Code => "draft_payment";

    public override string Description =>
        "To'lov yozuvining QORALAMASINI tayyorlaydi — yozuv YARATMAYDI. direction MAJBURIY: "
        + "in — pul bizga tushdi (qarz kamayadi), out — biz to'ladik. Gapdan yo'nalish aniq "
        + "bo'lmasa avval foydalanuvchidan so'rang, taxmin qilmang. Natijada balans "
        + "oldi va keyin ko'rsatiladi.";

    public override string PermissionCode => WmsPermissions.FinanceManage;

    public override string? FeatureCode => FeatureCodes.FinancePayments;

    protected override async Task<AiToolResult> RunAsync(
        DraftPaymentArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        if (args.Direction is not { } direction)
        {
            return new AiToolResult(
                "Yo'nalish ko'rsatilmagan. Pul BIZGA tushdimi (in) yoki BIZ to'ladikmi (out)? "
                + "Foydalanuvchidan so'rang — taxmin qilmang.");
        }

        if (args.Amount <= 0)
        {
            return new AiToolResult("Summa ko'rsatilmagan yoki noldan katta emas — foydalanuvchidan so'rang.");
        }

        Guid counterpartyId;
        string counterpartyName;

        if (args.CounterpartyId is { } id)
        {
            counterpartyId = id;
            counterpartyName = "—";
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

            counterpartyId = resolved.Match!.Id;
            counterpartyName = resolved.Match.Name;
        }
        else
        {
            return new AiToolResult("Kontragent ko'rsatilmagan — kim to'lagani/kimga to'langanini so'rang.");
        }

        List<DebtDto> debts = await _finance.GetDebtsAsync();
        DebtDto? debt = debts.FirstOrDefault(d => d.CounterpartyId == counterpartyId);

        if (debt is not null && counterpartyName == "—")
        {
            counterpartyName = debt.CounterpartyName;
        }

        decimal before = debt?.Amount ?? 0m;

        // ⚠️ `FinanceService.DebtDelta` bilan AYNAN bir xil qoida: pul bizga tushsa qarz
        // kamayadi. Ikki joyda ayri hisoblansa, qoralamadagi «keyin» yozuvdan keyingi
        // haqiqiy balansdan farq qilardi — va odam nimaga ishonishini bilmasdi.
        decimal after = before + (direction == PaymentDirection.In ? -args.Amount : args.Amount);

        AiPaymentDraft draft = new(
            counterpartyId,
            counterpartyName,
            args.Amount,
            direction,
            args.Method ?? PaymentMethod.Cash,
            args.DocumentDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.Date,
            before,
            after,
            args.Note);

        return new AiToolResult(Describe(draft), draft);
    }

    private static string Describe(AiPaymentDraft draft)
    {
        string direction = draft.Direction == PaymentDirection.In
            ? "pul BIZGA tushdi (qarz kamayadi)"
            : "BIZ to'ladik (qarz oshadi)";

        List<string> lines =
        [
            "TO'LOV qoralamasi (yozuv HALI kiritilmadi):",
            $"- Kontragent: {draft.CounterpartyName}",
            $"- Summa: {Money(draft.Amount)} — {direction}",
            $"- Usul: {draft.Method}",
            $"- Sana: {Date(draft.DocumentDate)}",
            $"- Balans: {Balance(draft.BalanceBefore)} → {Balance(draft.BalanceAfter)}",
            "Foydalanuvchiga ko'rsating va tasdiqlashni SO'RANG — yozuvni o'zingiz kirita olmaysiz.",
        ];

        return string.Join("\n", lines);
    }

    /// <summary>Balansni ISHORA bilan emas, SO'Z bilan yozadi.</summary>
    /// <remarks>
    /// Model «-500 000» ni «qarzdor» deb o'qib, javobni teskarisiga aylantirib yuborardi.
    /// </remarks>
    private static string Balance(decimal amount) => amount switch
    {
        > 0 => $"bizga qarzdor {Money(amount)}",
        < 0 => $"biz qarzdormiz {Money(-amount)}",
        _ => "nol",
    };
}
