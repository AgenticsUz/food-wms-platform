namespace WMS.API.Surfaces;

/// <summary>
/// WMS API'sining IKKI yuzasi (PLATFORMA-TZ §4.4, Wash naqshi).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ApiPrefix"/> — zavodning o'z ilovasi (<c>wms-web</c>), tenant tokendagi
/// <c>tenant_id</c> dan. <see cref="AdminPrefix"/> — Console'ning WMS bo'limi: tenant
/// OSHKORA tanlanadi (<c>X-Tenant-Id</c> yoki marshrut), chunki Console tokenida
/// <c>tenant_id</c> yo'q.
/// </para>
/// <para>
/// ⚠️ Chegarani AUDIENCE ushlab turadi, prefiks emas: ikki yuzaning audience'i ajralgan
/// va <c>wms-web</c> tokeni admin yuzasida umuman tekshiruvdan o'tmaydi — 403 emas, 401
/// (§5.6-5). Ikkalasi BITTA jarayonda bo'lgani uchun ikki nomlangan JWT sxemasi va yo'l
/// bo'yicha yo'naltiruvchi standart sxema kerak (<c>WmsAuthenticationExtensions</c>).
/// </para>
/// <para>
/// ⚠️ Admin prefiksi ILDIZDA, <c>/api</c> ostida EMAS: Console <c>/api/wms/admin/v1/...</c>
/// ga boradi va nginx <c>/api/wms/</c> ni kesib tashlaydi.
/// </para>
/// </remarks>
public static class WmsSurfaces
{
    /// <summary>Mahsulot kodi — Identity katalogida va Console proksisida (<c>/api/wms/</c>).</summary>
    public const string ProductCode = "wms";

    public const string AdminVersion = "v1";
    public const string ApiPrefix = "/api";
    public const string AdminPrefix = "/admin/" + AdminVersion;

    public const string ApiScheme = "wms-jwt";
    public const string AdminScheme = "wms-admin-jwt";

    /// <summary>Console yuzasining avtorizatsiya siyosati.</summary>
    public const string AdminPolicy = "wms-admin-elevated";

    /// <summary>Console'ning WMS administratori roli (§4.2).</summary>
    public const string AdminRole = "wms.admin";

    /// <summary>Audience kalitlari — KO'PLIKDA (<c>Auth:Audiences:*</c>): birlikdagi kalit HRM'da muhitdan kelgan qiymatni jimgina yutgan.</summary>
    public const string ApiAudienceKey = "Auth:Audiences:Wms";
    public const string AdminAudienceKey = "Auth:Audiences:Admin";

    public const string ApiAudience = "wms-api";

    /// <summary>
    /// Console yuzasining audience'i. ⚠️ Mahsulotga XOS nom: oddiy <c>admin-api</c> HRM'niki
    /// bilan ustma-ust tushib, Console'ning HRM tokeni WMS admin yuzasini ochardi.
    /// </summary>
    public const string AdminAudience = "wms-admin-api";

    /// <summary>Yuza tirikligining «sog'lom» qiymati (Console shu satrni kutadi).</summary>
    public const string HealthyStatus = "healthy";

    public static bool IsAdminPath(PathString path) =>
        path.StartsWithSegments(AdminPrefix, StringComparison.OrdinalIgnoreCase);
}

/// <summary><c>GET /admin/v1/health</c> javobi (Console'ning <c>ProductSurfaceHealth</c>).</summary>
public sealed record WmsSurfaceHealth(string Surface, string Version, string Status, DateTimeOffset Utc);
