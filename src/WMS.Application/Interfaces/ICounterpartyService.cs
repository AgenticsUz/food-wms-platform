using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface ICounterpartyService
{
    Task<List<CounterpartyDto>> GetAllAsync(CounterpartyType? type = null);
    Task<CounterpartyDto> GetByIdAsync(Guid id);
    Task<CounterpartyDto> CreateAsync(CreateCounterpartyDto dto);
    Task<CounterpartyDto> UpdateAsync(Guid id, UpdateCounterpartyDto dto);
    Task DeleteAsync(Guid id);
    Task<CounterpartyBalanceDto> GetBalanceAsync(Guid counterpartyId);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid counterpartyId);
}
