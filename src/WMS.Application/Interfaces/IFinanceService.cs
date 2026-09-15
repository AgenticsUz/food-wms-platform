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
    /// <param name="dto">To'lov ma'lumoti.</param>
    /// <param name="source">Qaysi yuzadan (UI / Telegram / AI) — DTO'dan EMAS, chaqiruvchidan.</param>
    /// <returns>Saqlangan to'lov.</returns>
    Task<PaymentHistoryDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto,
        DocumentSource source = DocumentSource.Ui);

    /// <summary>To'lovni QAYTARADI (storno): asl yozuv o'chmaydi, teskari yozuv qo'shiladi.</summary>
    /// <param name="userId">Qaytargan <c>user_profile.id</c>.</param>
    /// <param name="paymentId">Asl to'lov.</param>
    /// <param name="reason">Sabab (tarixda ko'rinadi).</param>
    /// <param name="source">Qaysi yuzadan.</param>
    /// <returns>Storno yozuvi.</returns>
    /// <remarks>
    /// Nega o'chirish emas: pul yozuvi «jimgina» yo'qolsa, kechagi hisobot bugun boshqacha
    /// bo'lib qolardi va sababini hech kim topa olmasdi. Bir to'lov IKKI marta
    /// qaytarilmaydi (noyob indeks).
    /// </remarks>
    Task<PaymentHistoryDto> ReversePaymentAsync(Guid userId, Guid paymentId, string reason,
        DocumentSource source = DocumentSource.Ui);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid? counterpartyId = null, int page = 1, int pageSize = 50);
    Task<FinanceSummaryDto> GetSummaryAsync();
}
