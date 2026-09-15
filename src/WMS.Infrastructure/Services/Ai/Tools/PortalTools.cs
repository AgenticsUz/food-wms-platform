using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Portal;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Kabinet hujjatlari argumentlari.</summary>
public sealed class MyTransfersArgs
{
    /// <summary>Ko'pi bilan nechta hujjat (sukut 20).</summary>
    public int? Limit { get; set; }
}

/// <summary>
/// <c>my_debt</c> — kabinet foydalanuvchisining O'Z balansi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Kontragent id'si argument EMAS va hech qachon bo'lmaydi.</b> Kim so'rayotgani
/// tokendagi <c>sub</c> dan yechiladi (<see cref="IPortalService"/>), ya'ni «boshqasining
/// id'sini qo'yib ko'raman» hujumi uchun yuza yo'q. Bu qoida kabinet yuzasining o'zida ham
/// shunday (<c>PortalController</c> izohi) — AI uni buzmasligi kerak.
/// </para>
/// <para>
/// Ruxsat kodi <see cref="WmsPermissions.PortalSelf"/>: u RBAC katalogida YO'Q, ya'ni hech
/// bir xodim rolida uchramaydi. Demak bu tool zavod xodimiga umuman ko'rinmaydi, kabinet
/// foydalanuvchisiga esa faqat shu va shunga o'xshash tool'lar ko'rinadi.
/// </para>
/// </remarks>
public sealed class MyDebtTool : AiTool<NoArgs>
{
    private readonly IPortalService _portal;

    public MyDebtTool(IPortalService portal) => _portal = portal;

    public override string Code => "my_debt";

    public override string Description =>
        "Foydalanuvchining O'Z balansi: qarz, umumiy aylanma va to'langan summa.";

    public override string PermissionCode => WmsPermissions.PortalSelf;

    protected override async Task<AiToolResult> RunAsync(
        NoArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        PortalFinanceDto finance = await _portal.GetFinanceAsync(cancellationToken);

        // ⚠️ Ishora ma'noni o'zgartiradi (`DebtLedger` qoidasi): musbat — mijoz qarzdor.
        // So'z bilan yozilmasa model «-500 000» ni «qarzdor» deb o'qib yuborardi.
        string balance = finance.DebtAmount switch
        {
            > 0 => $"Sizning qarzingiz: {Money(finance.DebtAmount)}",
            < 0 => $"Zavod sizga qarzdor: {Money(-finance.DebtAmount)}",
            _ => "Balans nol — qarz yo'q.",
        };

        string text = string.Join("\n",
            balance,
            $"- Umumiy aylanma: {Money(finance.TotalTurnover)}",
            $"- To'langan: {Money(finance.TotalPaid)}",
            finance.LastPaymentAt is { } last ? $"- Oxirgi to'lov: {Date(last)}" : "- To'lov tarixi yo'q");

        return new AiToolResult(text, finance);
    }
}

/// <summary>
/// <c>my_transfers</c> — kabinet foydalanuvchisining O'Z hujjatlari.
/// </summary>
/// <remarks>
/// Natija <see cref="PortalTransferDto"/> — ilovaning <c>TransferDto</c> si EMAS: unda
/// ichki ma'lumot bor (qaysi ombor, kim yaratdi, agent foizi) va ba'zisi tijorat siri.
/// </remarks>
public sealed class MyTransfersTool : AiTool<MyTransfersArgs>
{
    private const int DefaultLimit = 20;

    private readonly IPortalService _portal;

    public MyTransfersTool(IPortalService portal) => _portal = portal;

    public override string Code => "my_transfers";

    public override string Description =>
        "Foydalanuvchining O'Z hujjatlari (yangi sana birinchi): raqam, sana, summa, holat.";

    public override string PermissionCode => WmsPermissions.PortalSelf;

    protected override async Task<AiToolResult> RunAsync(
        MyTransfersArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(args.Limit ?? DefaultLimit, 1, MaxRows);

        List<PortalTransferDto> transfers = await _portal.GetTransfersAsync(1, limit, cancellationToken);
        if (transfers.Count == 0)
        {
            return new AiToolResult("Hujjat topilmadi.");
        }

        string text = string.Join("\n", transfers.Select(t =>
            $"- #{t.Number}, {Date(t.DocumentDate)}: {Money(t.TotalAmount)} ({t.Status})"));

        return new AiToolResult($"{transfers.Count} ta hujjat:\n{text}", transfers);
    }
}
