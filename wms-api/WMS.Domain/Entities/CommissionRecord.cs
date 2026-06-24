using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class CommissionRecord : BaseEntity
{
    public int TenantId { get; set; }
    public int AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    public int TransferId { get; set; }
    public Transfer Transfer { get; set; } = null!;
    public decimal SaleAmount { get; set; }        // sale total (snapshot)
    public decimal CommissionPercent { get; set; } // applied percent (snapshot)
    public decimal CommissionAmount { get; set; }  // computed commission
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending; // client-payment state
    public bool IsPaid { get; set; } = false;      // paid out to the agent
    public DateTime? PaidAt { get; set; }
}
