using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Transaction : BaseEntity
{
    public int TenantId { get; set; }
    public TransactionType Type { get; set; }
    public int? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }
    public int? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
}
