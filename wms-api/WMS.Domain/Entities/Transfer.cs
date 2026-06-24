using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Transfer : BaseEntity
{
    public int TenantId { get; set; }
    public TransferType Type { get; set; }
    public int? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public int? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public int? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }
    public int? AgentId { get; set; }                 // sale via agent (optional)
    public Agent? Agent { get; set; }
    public decimal? CommissionPercent { get; set; }   // override of agent percent for this sale
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public ICollection<TransferItem> Items { get; set; } = new List<TransferItem>();
}
