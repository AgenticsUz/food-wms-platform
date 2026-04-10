using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Debt : BaseEntity
{
    public int TenantId { get; set; }
    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;
    public decimal Amount { get; set; }
}
