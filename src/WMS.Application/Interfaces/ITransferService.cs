using WMS.Application.DTOs.Transfers;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

// Tenant parametri yo'q (D4): WmsDbContext filtri + RLS joriy tenantni o'zi qo'yadi.
public interface ITransferService
{
    Task<List<TransferDto>> GetAllAsync(TransferType? type = null,
        TransferStatus? status = null, DateTime? from = null, DateTime? to = null,
        Guid? counterpartyId = null, int page = 1, int pageSize = 50);
    Task<TransferDto> GetByIdAsync(Guid id);

    /// <param name="userId">Yaratuvchining <c>user_profile.id</c> si.</param>
    Task<TransferDto> CreateAsync(Guid userId, CreateTransferDto dto);
    Task<TransferDto> ConfirmAsync(Guid id);
    Task<TransferDto> RejectAsync(Guid id);
    Task CancelAsync(Guid id);
}
