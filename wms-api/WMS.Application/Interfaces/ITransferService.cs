using WMS.Application.DTOs.Transfers;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface ITransferService
{
    Task<List<TransferDto>> GetAllAsync(int tenantId, TransferType? type = null,
        TransferStatus? status = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 50);
    Task<TransferDto> GetByIdAsync(int tenantId, int id);
    Task<TransferDto> CreateAsync(int tenantId, int userId, CreateTransferDto dto);
    Task<TransferDto> ConfirmAsync(int tenantId, int id);
    Task<TransferDto> RejectAsync(int tenantId, int id);
    Task CancelAsync(int tenantId, int id);
}
