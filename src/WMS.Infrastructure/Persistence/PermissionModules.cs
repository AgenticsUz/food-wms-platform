using WMS.Application.Common;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Ruxsat guruhi (<see cref="PermissionDefinition.Group"/>) va <see cref="ModuleCodes"/> **bir xil emas** —
/// shu sababli bu jadval kerak.
///
/// Farqlar:
/// <list type="bullet">
/// <item>`WAREHOUSE` bitta ruxsat guruhi, lekin ikkita modul bor
///   (`WAREHOUSE_RAW`, `WAREHOUSE_FINISHED`) — istalgan biri yoqilgan bo'lsa yetarli.</item>
/// <item>`PARTNERS` ham shunday: `SUPPLIERS` yoki `CLIENTS`.</item>
/// <item>`DASHBOARD`, `PRODUCTS`, `SETTINGS` — hech qanday modulga bog'lanmagan.
///   Ular yadro qismi va har doim mavjud (obuna nima bo'lishidan qat'i nazar).</item>
/// </list>
///
/// Nomlarni birxillashtirish o'rniga jadval yozildi: guruh UI'dagi sarlavha ham, uni modul
/// kodiga tenglashtirish ikkita `WAREHOUSE` guruhi paydo bo'lishiga olib kelardi.
/// </summary>
/// <remarks>
/// F6: modul to'plami endi Identity obunasidan (tenant nusxasidagi <c>modules</c>, D6) — jadvalning
/// o'zi o'zgarmadi. Ma'nosi saqlandi: rollar ekrani obunaga kirmaydigan ruxsatni kulrang ko'rsatadi
/// va saqlashda ogohlantiradi (B5), <c>[RequireModule]</c> esa baribir 403 beradi.
/// </remarks>
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
    /// Ruxsat guruhi tenantda amalda ishlaydimi. Modulga bog'lanmagan guruhlar
    /// (`DASHBOARD`, `PRODUCTS`, `SETTINGS`) har doim `true`.
    /// </summary>
    public static bool IsAvailable(string? permissionGroup, IReadOnlySet<string> enabledModuleCodes)
    {
        if (string.IsNullOrWhiteSpace(permissionGroup)) return true;
        if (!Map.TryGetValue(permissionGroup, out var required)) return true;
        return required.Any(enabledModuleCodes.Contains);
    }

    /// <summary>Ruxsat KODI bo'yicha (guruh katalogdan). Katalogda yo'q kod — mavjud emas.</summary>
    public static bool IsCodeAvailable(string permissionCode, IReadOnlySet<string> enabledModuleCodes)
    {
        var definition = WmsPermissions.All.FirstOrDefault(p => string.Equals(p.Code, permissionCode, StringComparison.Ordinal));
        return definition is not null && IsAvailable(definition.Group, enabledModuleCodes);
    }
}
