using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// A sellable capability, one level finer than a module: "production.recipes" rather than
/// the whole PRODUCTION module. Platform catalog — no TenantId.
///
/// Layers, coarse to fine: Plan (what was sold) → Feature (what this tenant has) →
/// Permission (who inside the tenant may use it).
/// </summary>
public class Feature : BaseEntity
{
    public string Code { get; set; } = null!;        // unique, e.g. "production.recipes"
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// The module this feature belongs to. A feature can never be on while its module is off.
    /// Null for cross-cutting features (export, import, analytics) that no single module owns.
    public string? ModuleCode { get; set; }

    /// Value used when neither a tenant override nor the plan says anything.
    public bool DefaultEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    // ── Custom features (S5): written for one specific tenant ──
    /// True for one-customer features. Must have DefaultEnabled = false and must never
    /// appear in a plan's FeatureCodes — enforced in FeatureService.
    public bool IsCustom { get; set; }
    public int? OwnerTenantId { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Per-tenant override of a feature. Present = decisive (beats the plan); absent = fall back
/// to the plan, then to <see cref="Feature.DefaultEnabled"/>.
/// </summary>
public class TenantFeature : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string FeatureCode { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string? Note { get; set; }

    public int? SetByUserId { get; set; }
    public DateTime SetAt { get; set; } = DateTime.UtcNow;
}
