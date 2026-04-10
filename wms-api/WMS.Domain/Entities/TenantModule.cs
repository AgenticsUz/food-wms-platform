using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class TenantModule : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}
