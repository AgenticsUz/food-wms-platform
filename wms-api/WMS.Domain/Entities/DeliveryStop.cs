using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class DeliveryStop : BaseEntity
{
    public int TenantId { get; set; }
    public int DeliveryId { get; set; }
    public Delivery Delivery { get; set; } = null!;
    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;
    public int? TransferId { get; set; }         // the outgoing sale being delivered (optional)
    public Transfer? Transfer { get; set; }
    public string? Address { get; set; }
    public int SequenceOrder { get; set; }
    public DeliveryStopStatus Status { get; set; } = DeliveryStopStatus.Pending;
    public DateTime? DeliveredAt { get; set; }
    public string? Note { get; set; }
}
