using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Helpers for mapping a plan's ModuleCodes CSV ↔ List&lt;string&gt; and applying a plan's
/// module set to a tenant's TenantModule rows (control-plane module gating).
/// </summary>
public static class PlanModules
{
    public static List<string> Split(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new List<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .ToList();

    public static string Join(IEnumerable<string>? codes) =>
        codes == null
            ? ""
            : string.Join(",", codes.Select(c => c?.Trim()).Where(c => !string.IsNullOrEmpty(c)));

    /// <summary>
    /// For every system Module, set the tenant's TenantModule.IsEnabled = (module is in the plan's set),
    /// creating the TenantModule row when missing.
    /// </summary>
    public static async Task ApplyPlanModulesAsync(WmsDbContext db, int tenantId, Plan plan)
    {
        var allowed = new HashSet<string>(Split(plan.ModuleCodes), StringComparer.OrdinalIgnoreCase);

        var modules = await db.Modules.ToListAsync();
        var tenantModules = await db.TenantModules
            .Where(tm => tm.TenantId == tenantId)
            .ToListAsync();

        foreach (var module in modules)
        {
            var isEnabled = allowed.Contains(module.Code);
            var tm = tenantModules.FirstOrDefault(x => x.ModuleId == module.Id);
            if (tm != null)
                tm.IsEnabled = isEnabled;
            else
                db.TenantModules.Add(new TenantModule
                    { TenantId = tenantId, ModuleId = module.Id, IsEnabled = isEnabled });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Plandan chiqarilgan (plansiz) tenant uchun: barcha modullar yoqiladi.
    /// Kelishuv: PLANI BOR tenant → modullar plandan; PLANI YO'Q tenant (tizim tenanti,
    /// eski yozuvlar, maxsus shartnoma) → cheklovsiz. Aks holda plan olib tashlanganda
    /// eski planning to'plami "muzlab" qolardi.
    /// </summary>
    public static async Task ApplyAllModulesAsync(WmsDbContext db, int tenantId)
    {
        var modules = await db.Modules.ToListAsync();
        var tenantModules = await db.TenantModules
            .Where(tm => tm.TenantId == tenantId)
            .ToListAsync();

        foreach (var module in modules)
        {
            var tm = tenantModules.FirstOrDefault(x => x.ModuleId == module.Id);
            if (tm != null)
                tm.IsEnabled = true;
            else
                db.TenantModules.Add(new TenantModule
                    { TenantId = tenantId, ModuleId = module.Id, IsEnabled = true });
        }
        await db.SaveChangesAsync();
    }
}
