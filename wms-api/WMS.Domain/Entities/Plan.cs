using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// Platform-global subscription plan (control plane). NO TenantId — plans are shared
/// across the whole platform. A tenant references a Plan via <see cref="Tenant.PlanId"/>.
/// </summary>
public class Plan : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;              // unique-ish per platform, e.g. "basic", "pro"
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public string ModuleCodes { get; set; } = "";          // CSV of module codes, e.g. "WAREHOUSE_RAW,TRANSFERS"
    public int MaxUsers { get; set; } = 10;
    public int MaxWarehouses { get; set; } = 3;
    public int MaxTransfersPerMonth { get; set; } = 1000;
}
