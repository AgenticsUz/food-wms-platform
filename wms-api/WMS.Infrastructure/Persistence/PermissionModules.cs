using WMS.Application.Common;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// `Permission.Module` va `ModuleCodes` **bir xil emas** — shu sababli bu jadval kerak.
///
/// Farqlar:
/// <list type="bullet">
/// <item>`WAREHOUSE` bitta ruxsat guruhi, lekin ikkita modul bor
///   (`WAREHOUSE_RAW`, `WAREHOUSE_FINISHED`) — istalgan biri yoqilgan bo'lsa yetarli.</item>
/// <item>`PARTNERS` ham shunday: `SUPPLIERS` yoki `CLIENTS`.</item>
/// <item>`DASHBOARD`, `PRODUCTS`, `SETTINGS` — hech qanday modulga bog'lanmagan.
///   Ular yadro qismi va har doim mavjud (plan nima bo'lishidan qat'i nazar).</item>
/// </list>
///
/// Nomlarni birxillashtirish o'rniga jadval yozildi: `Permission.Module` UI'dagi
/// guruh sarlavhasi ham, uni modul kodiga tenglashtirish ikkita `WAREHOUSE` guruhi
/// paydo bo'lishiga olib kelardi.
/// </summary>
public static class PermissionModules
{
    /// Ruxsat guruhi → uni yoqadigan modul kodlari. Ro'yxatda **kamida bittasi**
    /// yoqilgan bo'lsa ruxsat mavjud hisoblanadi.
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WAREHOUSE"]  = [ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished],
        ["PARTNERS"]   = [ModuleCodes.Suppliers, ModuleCodes.Clients],
        ["PRODUCTION"] = [ModuleCodes.Production],
        ["TRANSFERS"]  = [ModuleCodes.Transfers],
        ["FINANCE"]    = [ModuleCodes.Finance],
        ["KPI"]        = [ModuleCodes.Kpi],
        ["QUALITY"]    = [ModuleCodes.Quality],
        ["AGENTS"]     = [ModuleCodes.Agents],
        ["DELIVERY"]   = [ModuleCodes.Delivery]
    };

    /// <summary>
    /// Ruxsat tenantda amalda ishlaydimi. Modulga bog'lanmagan guruhlar
    /// (`DASHBOARD`, `PRODUCTS`, `SETTINGS`) har doim `true`.
    /// </summary>
    public static bool IsAvailable(string? permissionModule, IReadOnlySet<string> enabledModuleCodes)
    {
        if (string.IsNullOrWhiteSpace(permissionModule)) return true;
        if (!Map.TryGetValue(permissionModule, out var required)) return true;
        return required.Any(enabledModuleCodes.Contains);
    }
}
