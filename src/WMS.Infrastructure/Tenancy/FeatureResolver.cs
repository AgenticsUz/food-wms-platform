using Microsoft.EntityFrameworkCore;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Tenancy;

/// <summary>Feature qiymati qayerdan kelgani — Console operatoriga «plan beradi»ni «tenantga berilgan»dan ajratib ko'rsatadi.</summary>
public enum FeatureSource
{
    /// <summary>Katalog sukuti (plan ham, override ham yo'q).</summary>
    Default,

    /// <summary>Tenant plani o'z ichiga oladi.</summary>
    Plan,

    /// <summary>Tenant uchun oshkora override.</summary>
    Tenant,

    /// <summary>Egasi modul o'chiq bo'lgani uchun majburan o'chiq.</summary>
    Module,
}

public sealed record ResolvedFeature(
    string Code, string Name, string? ModuleCode, bool IsEnabled,
    FeatureSource Source, string? Note, bool IsCustom, int SortOrder,
    Guid? OwnerTenantId = null, DateTime? RequestedAt = null, string? Reason = null);

/// <summary>
/// Feature qatlamini AYNAN shu tartibda yechadi: (1) tenant override; (2) tenant plani;
/// (3) katalog sukuti; (4) har holda — egasi modul o'chiq bo'lsa feature ham o'chiq
/// (modul — yirikroq qatlam, feature undan o'tib ketolmaydi).
/// </summary>
/// <remarks>
/// F6: modul to'plami endi Identity obunasidan (tenant nusxasidagi <c>modules</c>), SQLite
/// davridagi <c>TenantModule</c> jadvalidan emas (D6). Override'lar RLS ostida — chaqiruvchi
/// tenant kontekstini o'rnatgan bo'lishi SHART (<c>ITenantStateService</c> izohi).
/// </remarks>
public static class FeatureResolver
{
    public static async Task<List<ResolvedFeature>> ResolveAsync(
        WmsDbContext db, Guid tenantId, Guid? planId, IReadOnlySet<string> enabledModules, CancellationToken ct = default)
    {
        var catalog = await db.Features.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct);
        if (catalog.Count == 0)
        {
            return [];
        }

        HashSet<string>? planCodes = null;
        if (planId is { } id)
        {
            string? csv = await db.Plans.AsNoTracking().Where(p => p.Id == id).Select(p => p.FeatureCodes).FirstOrDefaultAsync(ct);
            planCodes = new HashSet<string>(PlanModules.Split(csv), StringComparer.OrdinalIgnoreCase);
        }

        var overrides = await db.TenantFeatures.AsNoTracking()
            .Where(tf => tf.TenantId == tenantId)
            .ToListAsync(ct);

        List<ResolvedFeature> result = new(catalog.Count);
        foreach (var feature in catalog)
        {
            var over = overrides.FirstOrDefault(o => string.Equals(o.FeatureCode, feature.Code, StringComparison.OrdinalIgnoreCase));

            bool enabled;
            FeatureSource source;
            if (over is not null)
            {
                enabled = over.IsEnabled;
                source = FeatureSource.Tenant;
            }
            else if (planCodes is not null)
            {
                enabled = planCodes.Contains(feature.Code);
                source = FeatureSource.Plan;
            }
            else
            {
                enabled = feature.DefaultEnabled;
                source = FeatureSource.Default;
            }

            if (enabled && feature.ModuleCode is not null && !enabledModules.Contains(feature.ModuleCode))
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
}
