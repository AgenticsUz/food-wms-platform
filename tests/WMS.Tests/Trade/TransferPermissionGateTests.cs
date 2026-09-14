using System.Reflection;
using WMS.API.Controllers.Trade;
using WMS.API.Middleware;
using WMS.Application.Common;

namespace WMS.Tests.Trade;

/// <summary>
/// Transfer endpoint'larining ruxsat darvozasi: kim qanday atribut bilan qo'riqlanadi va
/// tizim rollariga nima berilgan.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Nima uchun servis darajasida 403 testi YO'Q. Ruxsat <c>TransferService</c> da
/// EMAS, controller'da (<see cref="RequirePermissionAttribute"/>) tekshiriladi — servis
/// chaqiruvchi kim ekanini umuman bilmaydi. Shuning uchun servisga «ruxsatsiz foydalanuvchi»
/// bilan kirib 403 kutadigan test yolg'on xotirjamlik berardi: u hech qachon qizil bermasdi,
/// atribut olib tashlansa ham.
/// </para>
/// <para>
/// Bu yerdagi da'volar AYNAN shu bo'shliqni yopadi: refleksiya bilan atributning BORLIGI va
/// KODI, hamda <see cref="WmsSystemRoles.PermissionsFor"/> dagi boshlang'ich to'plam. Atribut
/// tasodifan o'chirilsa yoki kod almashsa (masalan tasdiqlash <c>transfers.create</c> ga
/// bog'lanib qolsa) — testlar darhol qizil beradi. Bu yagona statik darvoza: HTTP quvuri
/// ko'tarilmaydi, ya'ni test tez va tashqi bog'liqliksiz.
/// </para>
/// </remarks>
public sealed class TransferPermissionGateTests
{
    [Fact]
    public void Kuzatuvchi_rolida_transfer_YARATISH_ruxsati_yoq()
    {
        IReadOnlyList<string> viewer = WmsSystemRoles.PermissionsFor(WmsSystemRoles.Viewer);

        // Kuzatuvchi — faqat `.view` bilan tugaydigan ruxsatlar.
        viewer.ShouldContain(WmsPermissions.TransfersView);
        viewer.ShouldNotContain(WmsPermissions.TransfersCreate);
        viewer.ShouldNotContain(WmsPermissions.TransfersConfirm);
        viewer.ShouldNotContain(WmsPermissions.TransfersReject);

        // Kabinet rollari ilovaning hech bir ekranini ochmaydi (fail-closed, D8).
        WmsSystemRoles.PermissionsFor(WmsSystemRoles.Client).ShouldBeEmpty();
        WmsSystemRoles.PermissionsFor(WmsSystemRoles.Agent).ShouldBeEmpty();

        // Qarama-qarshi tomon: to'plam BUTUNLAY bo'shab qolgani uchun test yashil bo'lib
        // qolmasin — admin va menejerda bu ruxsat BOR.
        WmsSystemRoles.PermissionsFor(WmsSystemRoles.Admin).ShouldContain(WmsPermissions.TransfersCreate);
        WmsSystemRoles.PermissionsFor(WmsSystemRoles.Manager).ShouldContain(WmsPermissions.TransfersConfirm);

        // Xodim transfer yaratadi, lekin O'ZI tasdiqlay olmaydi (vazifalar ajratilishi).
        IReadOnlyList<string> employee = WmsSystemRoles.PermissionsFor(WmsSystemRoles.Employee);
        employee.ShouldContain(WmsPermissions.TransfersCreate);
        employee.ShouldNotContain(WmsPermissions.TransfersConfirm);
    }

    [Theory]
    [InlineData(nameof(TransfersController.Create), WmsPermissions.TransfersCreate)]
    [InlineData(nameof(TransfersController.Confirm), WmsPermissions.TransfersConfirm)]
    [InlineData(nameof(TransfersController.Reject), WmsPermissions.TransfersReject)]
    [InlineData(nameof(TransfersController.Cancel), WmsPermissions.TransfersCreate)]
    public void Transfer_ozgartiruvchi_amallar_oz_ruxsati_bilan_qoriqlanadi(string methodName, string permission)
    {
        MethodInfo? method = typeof(TransfersController).GetMethod(methodName,
            BindingFlags.Public | BindingFlags.Instance);
        method.ShouldNotBeNull();

        IEnumerable<string> required = method!
            .GetCustomAttributes<RequirePermissionAttribute>(inherit: false)
            .Select(a => a.Permission);

        required.ShouldContain(permission);
    }

    [Fact]
    public void Controller_darajasida_KORISH_ruxsati_har_amalga_talab_qilinadi()
    {
        // Class darajasidagi talab har action'ga qo'shiladi: o'zgartiruvchi amalda IKKALASI ham shart
        // (`RequirePermissionAttribute` izohi) — ya'ni bu yagona satr butun controller'ni yopadi.
        IEnumerable<string> classLevel = typeof(TransfersController)
            .GetCustomAttributes<RequirePermissionAttribute>(inherit: false)
            .Select(a => a.Permission);

        classLevel.ShouldContain(WmsPermissions.TransfersView);

        // Modul darvozasi: tenant obunasida `TRANSFERS` moduli yo'q bo'lsa endpoint umuman ochilmaydi.
        IEnumerable<RequireModuleAttribute> modules =
            typeof(TransfersController).GetCustomAttributes<RequireModuleAttribute>(inherit: false);
        modules.ShouldNotBeEmpty();
    }
}
