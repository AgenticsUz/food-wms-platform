using WMS.Application.DTOs.Portal;

namespace WMS.Application.Interfaces;

/// <summary>
/// Kabinet yuzasi (<c>/api/portal/*</c>) — kontragent va agentning O'Z ma'lumoti.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Har metod aktyorni tokendagi <c>sub</c> dan O'ZI yechadi va so'rovni AYNAN o'sha
/// kontragent/agent bo'yicha filtrlaydi. Aktyor id'si tashqaridan parametr sifatida
/// OLINMAYDI: shunda «boshqasining id'sini qo'ying» hujumi uchun yuza yo'q (tenant
/// izolyatsiyasi RLS bilan, kontragent izolyatsiyasi shu bilan).
/// </para>
/// <para>
/// Bog'lanish yo'q bo'lsa har metod 403 beradi (fail-closed): <c>client</c> roli bor,
/// lekin kartaga biriktirilmagan odam hech narsa ko'rmaydi.
/// </para>
/// </remarks>
public interface IPortalService
{
    /// <summary>Kabinet egasi kim (va qaysi zavodda).</summary>
    Task<PortalMeDto> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>Balans va qisqacha yakun.</summary>
    Task<PortalFinanceDto> GetFinanceAsync(CancellationToken cancellationToken = default);

    /// <summary>O'z hujjatlari, yangisidan eskisiga.</summary>
    Task<List<PortalTransferDto>> GetTransfersAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Bitta hujjat; begonasi — «topilmadi».</summary>
    Task<PortalTransferDto> GetTransferAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>O'z to'lovlari tarixi.</summary>
    Task<List<PortalPaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Agent ko'rsatkichlari; kontragent chaqirsa — 403.</summary>
    Task<PortalAgentSummaryDto> GetAgentSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Agentning o'z mijozlari; kontragent chaqirsa — 403.</summary>
    Task<List<PortalAgentClientDto>> GetAgentClientsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Kabinet hisobini biriktirish — tenant adminining yuzasi (kabinet egasiniki EMAS).
/// </summary>
public interface IPortalAccountService
{
    Task<PortalAccountDto> GetCounterpartyAccountAsync(Guid counterpartyId, CancellationToken cancellationToken = default);

    Task<PortalAccountDto> LinkCounterpartyAsync(Guid counterpartyId, Guid identitySub, CancellationToken cancellationToken = default);

    Task<PortalAccountDto> UnlinkCounterpartyAsync(Guid counterpartyId, CancellationToken cancellationToken = default);

    Task<PortalAccountDto> GetAgentAccountAsync(Guid agentId, CancellationToken cancellationToken = default);

    Task<PortalAccountDto> LinkAgentAsync(Guid agentId, Guid identitySub, CancellationToken cancellationToken = default);

    Task<PortalAccountDto> UnlinkAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
}
