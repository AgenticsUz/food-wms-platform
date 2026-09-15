using WMS.Application.DTOs.Transfers;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

// Tenant parametri yo'q (D4): WmsDbContext filtri + RLS joriy tenantni o'zi qo'yadi.
public interface ITransferService
{
    /// <param name="type">Hujjat turi.</param>
    /// <param name="status">Holat.</param>
    /// <param name="from">Boshlanish sanasi — <c>DocumentDate</c> bo'yicha (P2.3), <c>CreatedAt</c> emas.</param>
    /// <param name="to">Tugash sanasi — <c>DocumentDate</c> bo'yicha.</param>
    /// <param name="counterpartyId">Kontragent.</param>
    /// <param name="page">Sahifa.</param>
    /// <param name="pageSize">Sahifa hajmi.</param>
    /// <returns>Hujjatlar (yangi sana birinchi).</returns>
    Task<List<TransferDto>> GetAllAsync(TransferType? type = null,
        TransferStatus? status = null, DateTime? from = null, DateTime? to = null,
        Guid? counterpartyId = null, int page = 1, int pageSize = 50);
    Task<TransferDto> GetByIdAsync(Guid id);

    /// <param name="userId">Yaratuvchining <c>user_profile.id</c> si.</param>
    /// <param name="dto">Hujjat.</param>
    /// <param name="source">
    /// Hujjat qaysi yuzadan kirgani. ⚠️ DTO'da EMAS, parametr: manbani chaqiruvchi kod
    /// (controller, bot, AI oqimi) belgilaydi — mijoz o'zini «AI» deb ko'rsatolmaydi.
    /// </param>
    /// <returns>Yaratilgan hujjat.</returns>
    /// <param name="aiConversationId">
    /// AI qoralamasidan yaratilganda — o'sha suhbat (<c>Source = Ai</c> bilan birga).
    /// </param>
    /// <remarks>
    /// ⚠️ Suhbat id'si ham DTO'da emas, parametrda: mijoz o'z hujjatini begona suhbatga
    /// bog'lab, audit izini soxtalashtira olmasin.
    /// </remarks>
    Task<TransferDto> CreateAsync(
        Guid userId,
        CreateTransferDto dto,
        DocumentSource source = DocumentSource.Ui,
        Guid? aiConversationId = null);
    Task<TransferDto> ConfirmAsync(Guid id);
    Task<TransferDto> RejectAsync(Guid id);
    Task CancelAsync(Guid id);
}
