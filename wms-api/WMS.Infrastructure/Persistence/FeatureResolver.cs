using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

/// Where a feature's effective value came from — shown in the admin console so the
/// operator can tell "this tenant was given it" from "the plan includes it".
public enum FeatureSource
{
    Default,   // catalog default (tenant has no plan and no override)
    Plan,      // the tenant's plan lists it
    Tenant,    // explicit per-tenant override
    Module     // forced off because the owning module is disabled
}

public record ResolvedFeature(
    string Code, string Name, string? ModuleCode, bool IsEnabled,
    FeatureSource Source, string? Note, bool IsCustom, int SortOrder,
    int? OwnerTenantId = null, DateTime? RequestedAt = null, string? Reason = null);

/// <summary>
/// Resolves the feature layer, in this exact order:
///   1. a TenantFeature override decides;
///   2. otherwise the tenant's plan FeatureCodes decide;
///   3. otherwise the catalog default decides;
///   4. and in every case, a feature whose module is disabled is off — the module is the
///      coarser layer and a feature must never be able to reach past it.
/// </summary>
public static class FeatureResolver
{
    public static async Task<List<ResolvedFeature>> ResolveAsync(
        WmsDbContext db, int tenantId, HashSet<string> enabledModules, CancellationToken ct = default)
    {
        var catalog = await db.Features.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct);
        if (catalog.Count == 0) return [];

        var planCodes = await GetPlanFeatureCodesAsync(db, tenantId, ct);
        var overrides = await db.TenantFeatures.AsNoTracking()
            .Where(tf => tf.TenantId == tenantId)
            .ToListAsync(ct);

        var result = new List<ResolvedFeature>(catalog.Count);
        foreach (var feature in catalog)
        {
            var over = overrides.FirstOrDefault(o =>
                string.Equals(o.FeatureCode, feature.Code, StringComparison.OrdinalIgnoreCase));

            bool enabled;
            FeatureSource source;
            if (over != null)
            {
                enabled = over.IsEnabled;
                source = FeatureSource.Tenant;
            }
            else if (planCodes != null)
            {
                enabled = planCodes.Contains(feature.Code);
                source = FeatureSource.Plan;
            }
            else
            {
                enabled = feature.DefaultEnabled;
                source = FeatureSource.Default;
            }

            if (enabled && feature.ModuleCode != null && !enabledModules.Contains(feature.ModuleCode))
            {
                enabled = false;
                source = FeatureSource.Module;
            }

            result.Add(new ResolvedFeature(feature.Code, feature.Name, feature.ModuleCode,
                enabled, source, over?.Note, feature.IsCustom, feature.SortOrder,
                feature.OwnerTenantId, feature.RequestedAt, feature.Reason));
        }
        return result;
    }

    /// Null when the tenant has no plan (then the catalog default applies).
    private static async Task<HashSet<string>?> GetPlanFeatureCodesAsync(
        WmsDbContext db, int tenantId, CancellationToken ct)
    {
        var planId = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId).Select(t => t.PlanId).FirstOrDefaultAsync(ct);
        if (planId == null) return null;

        var csv = await db.Plans.AsNoTracking()
            .Where(p => p.Id == planId).Select(p => p.FeatureCodes).FirstOrDefaultAsync(ct);
        return new HashSet<string>(PlanModules.Split(csv), StringComparer.OrdinalIgnoreCase);
    }
}
