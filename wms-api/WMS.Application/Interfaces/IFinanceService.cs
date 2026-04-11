using WMS.Application.DTOs.Finance;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IFinanceService
{
    Task<List<TransactionDto>> GetTransactionsAsync(int tenantId, TransactionType? type = null,
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20);
    Task<TransactionDto> CreateTransactionAsync(int tenantId, int userId, CreateTransactionDto dto);
    Task<List<DebtDto>> GetDebtsAsync(int tenantId);
    Task<PaymentHistoryDto> CreatePaymentAsync(int tenantId, int userId, CreatePaymentDto dto);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(int tenantId, int? counterpartyId = null, int page = 1, int pageSize = 50);
    Task<FinanceSummaryDto> GetSummaryAsync(int tenantId);
}
