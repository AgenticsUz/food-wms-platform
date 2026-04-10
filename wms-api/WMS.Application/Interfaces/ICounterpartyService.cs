using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface ICounterpartyService
{
    Task<List<CounterpartyDto>> GetAllAsync(int tenantId, CounterpartyType? type = null);
    Task<CounterpartyDto> CreateAsync(int tenantId, CreateCounterpartyDto dto);
    Task<CounterpartyDto> UpdateAsync(int tenantId, int id, UpdateCounterpartyDto dto);
    Task DeleteAsync(int tenantId, int id);
    Task<CounterpartyBalanceDto> GetBalanceAsync(int tenantId, int counterpartyId);
    Task<List<PaymentHistoryDto>> GetPaymentsAsync(int tenantId, int counterpartyId);
}
