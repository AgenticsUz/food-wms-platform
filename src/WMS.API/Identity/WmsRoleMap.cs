using Platform.SharedKernel.Querying;
using Platform.Web.UserSync;
using WMS.Application.Common;

namespace WMS.API.Identity;

/// <summary>
/// Identity'ning YIRIK roli → WMS ruxsatlari (PLATFORMA-TZ §3.7, §5.2).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ WMS'da xarita AVTORITET EMAS: amaldagi huquqlarni <c>WmsAccessResolver</c>
/// <c>user_role → role_permission</c> dan yechadi va <c>IsAuthoritative = true</c> qaytaradi,
/// ya'ni paketning JIT middleware'i huquqlarni shu xaritadan YOZMAYDI. Xarita paket
/// shartnomasi uchun va to'plamlar <see cref="WmsSystemRoles"/> dan olinadi — JIT biriktiradigan
/// tizim rolining boshlang'ich ruxsatlari bilan bir manba.
/// </para>
/// <para>
/// Identity reyestrida <c>wms</c> mahsulotining rollari AYNAN <c>WmsSystemRoles.All</c>
/// bilan bir xil bo'lishi SHART (<see cref="WmsSystemRoles"/> izohi): xodim rollari va
/// kabinet rollari (<c>client</c>, <c>agent</c>) — oxirgilarining ruxsat to'plami bo'sh.
/// </para>
/// </remarks>
public static class WmsRoleMap
{
    public static RoleMap Declare()
    {
        RoleMap map = RoleMap.Declare()
            .For(WmsSystemRoles.Admin, [RoleMap.AllPermissions, .. WmsSystemRoles.PermissionsFor(WmsSystemRoles.Admin)]);

        // `Admin` yuqorida e'lon qilindi; qolgan hammasi (kabinet rollari ham — ularning
        // to'plami bo'sh) bir xil qoidada.
        foreach (string role in WmsSystemRoles.All.Where(r => r != WmsSystemRoles.Admin))
        {
            map.For(role, WmsSystemRoles.PermissionsFor(role), DataScope.All);
        }

        return map;
    }
}
