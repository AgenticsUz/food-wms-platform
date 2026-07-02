using WMS.Application.DTOs.Finance;
using WMS.Application.DTOs.Portal;
using WMS.Application.DTOs.Transfers;

namespace WMS.Application.Interfaces;

public interface IPortalAuthService
{
    Task<PortalAuthResponseDto> LoginAsync(PortalLoginDto dto);
    Task<PortalCounterpartyDto> GetCurrentAsync(int counterpartyId, int tenantId);
    Task<List<TransferDto>> GetTransfersAsync(int counterpartyId, int tenantId, int page = 1, int pageSize = 20);
    Task<TransferDto> GetTransferByIdAsync(int counterpartyId, int tenantId, int transferId);
    Task<PortalFinanceDto> GetFinanceAsync(int counterpartyId, int tenantId);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(int counterpartyId, int tenantId);
}
