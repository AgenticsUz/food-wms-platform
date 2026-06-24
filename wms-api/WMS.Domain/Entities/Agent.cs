using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Agent : BaseEntity
{
    public int TenantId { get; set; }
    public int? UserId { get; set; }          // optional system login account
    public User? User { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }   // 0..100
    public bool IsActive { get; set; } = true;

    // Portal (agent self-service cabinet)
    public string? PortalPhone { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool PortalEnabled { get; set; } = false;
}
