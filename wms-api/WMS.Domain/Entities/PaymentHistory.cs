using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class PaymentHistory : BaseEntity
{
    public int TenantId { get; set; }
    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;
    public int? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Note { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
}
