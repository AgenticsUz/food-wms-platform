using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface ICounterpartyService
{
    /// <summary>
    /// Kontragentlar ro'yxati; <paramref name="search"/> berilsa — nom bo'yicha taxminiy
    /// qidiruv (P2.1, <c>ISearchService</c>), tartib o'xshashlik bali bo'yicha.
    /// </summary>
    /// <param name="type">Mijoz/ta'minotchi filtri.</param>
    /// <param name="search">Qidiruv matni; bo'sh bo'lsa — oddiy ro'yxat (eski xatti-harakat).</param>
    /// <returns>Kontragentlar.</returns>
    Task<List<CounterpartyDto>> GetAllAsync(CounterpartyType? type = null, string? search = null);
    Task<CounterpartyDto> GetByIdAsync(Guid id);
    Task<CounterpartyDto> CreateAsync(CreateCounterpartyDto dto);
    Task<CounterpartyDto> UpdateAsync(Guid id, UpdateCounterpartyDto dto);
    Task DeleteAsync(Guid id);
    Task<CounterpartyBalanceDto> GetBalanceAsync(Guid counterpartyId);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid counterpartyId);
}
