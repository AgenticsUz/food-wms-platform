namespace WMS.Infrastructure.Persistence.Rls;

/// <summary>
/// Row Level Security uchun EF model annotatsiyalari (Wash/HRM naqshi).
/// </summary>
/// <remarks>
/// <see cref="Enabled"/> = <see langword="true"/> — <c>WmsDbContext</c> har
/// <c>ITenantEntity</c> ga avtomatik qo'yadi. Annotatsiya yo'q, lekin <c>tenant_id</c>
/// ustuni bor — xavfsizlik to'ri: RLS baribir qo'yiladi.
/// </remarks>
public static class RlsAnnotations
{
    /// <summary>RLS bayrog'i annotatsiya kaliti (bool).</summary>
    public const string Enabled = "wms:rls";

    /// <summary>Standart tenant ustuni.</summary>
    public const string DefaultTenantColumn = "tenant_id";

    /// <summary>Tenant qatorlari siyosati nomi.</summary>
    public const string PolicyName = "tenant_isolation";

    /// <summary>Sessiya o'zgaruvchisi — joriy tenant.</summary>
    public const string TenantSettingKey = "app.tenant_id";

    /// <summary>
    /// Joriy tenant SQL ifodasi. O'rnatilmagan bo'lsa NULL → siyosat 0 qator beradi (fail-closed).
    /// </summary>
    public const string CurrentTenantExpression =
        "NULLIF(current_setting('" + TenantSettingKey + "', true), '')::uuid";

    /// <summary>
    /// Tenant siyosati: <c>tenant_id = joriy_tenant</c>.
    /// </summary>
    /// <remarks>
    /// ⚠️ NULL escape YO'Q (HRM'dagi teshik Wash'da yopilgan). WMS'da <c>tenant_id</c>
    /// hamma tenant jadvalida NOT NULL — platforma qatorlari (plan, feature katalogi)
    /// alohida, RLS'siz jadvallarda turadi.
    /// </remarks>
    public static string BuildPolicyExpression(string tenantColumn) =>
        $"{tenantColumn} = {CurrentTenantExpression}";
}
