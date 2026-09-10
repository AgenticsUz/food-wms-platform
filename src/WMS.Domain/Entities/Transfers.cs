using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Transfer : TenantEntity
{
    public TransferType Type { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public Guid? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    /// <summary>Agent orqali sotuv (ixtiyoriy).</summary>
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    /// <summary>Shu sotuv uchun agent foizining override'i.</summary>
    public decimal? CommissionPercent { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public UserProfile? CreatedByUser { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary><c>Type == Return</c> bo'lganda.</summary>
    public ReturnReason? ReturnReason { get; set; }

    /// <summary>Qaytarilayotgan sotuv (ixtiyoriy).</summary>
    public Guid? OriginalTransferId { get; set; }

    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public ICollection<TransferItem> Items { get; set; } = new List<TransferItem>();
}

/// <summary>⚠️ F6 da <c>tenant_id</c> qo'shildi (D4) — sabab <see cref="Location"/> da.</summary>
public class TransferItem : TenantEntity
{
    public Guid TransferId { get; set; }
    public Transfer Transfer { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
