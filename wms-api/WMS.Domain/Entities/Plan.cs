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

    /// CSV of feature codes included in this plan, same style as ModuleCodes.
    /// A tenant-level TenantFeature row overrides whatever this says.
    public string FeatureCodes { get; set; } = "";

    /// The plan self-service registration attaches to a brand-new tenant (the trial plan).
    /// Exactly one plan should carry this flag — PlanService clears it from the others.
    public bool IsDefault { get; set; }

    /// Length of the trial for tenants that start on this plan. 0 = not a trial plan.
    public int TrialDays { get; set; }

    public int MaxUsers { get; set; } = 10;
    public int MaxWarehouses { get; set; } = 3;
    public int MaxTransfersPerMonth { get; set; } = 1000;
}
