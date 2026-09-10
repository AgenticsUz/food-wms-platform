using WMS.Application.DTOs.Finance;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IFinanceService
{
    Task<List<TransactionDto>> GetTransactionsAsync(TransactionType? type = null,
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20);

    /// <param name="userId">Yozgan <c>user_profile.id</c>.</param>
    Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto dto);
    Task DeleteTransactionAsync(Guid id);
    Task<List<DebtDto>> GetDebtsAsync();

    /// <param name="userId">Yozgan <c>user_profile.id</c>.</param>
    Task<PaymentHistoryDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid? counterpartyId = null, int page = 1, int pageSize = 50);
    Task<FinanceSummaryDto> GetSummaryAsync();
}
