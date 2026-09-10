using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

namespace WMS.API.Middleware;

// ⚠️ Atributlarning NOMI va NAMESPACE'i SQLite davridagidek qoldi (D2 — ko'chirish): 218 endpointli
// controller'lar `[RequirePermission]`, `[RequireModule]`, `[RequireFeature]` ni o'zgarishsiz
// saqlaydi. O'zgargani — MANBA: ruxsat so'rov boshida yechilgan to'plamdan (bazaga JOIN yo'q),
// modul Identity obunasidan (D6), tenant tokendan.

/// <summary>
/// Nozik ruxsat talabi. Class darajasida — har action; mutatsiyaga ikkinchisini qo'shsa — IKKALASI ham shart.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    public RequirePermissionAttribute(string permission) => Permission = permission;

    public string Permission { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Action'dagi `[RequireAnyPermission]` shu ruxsatni o'z ro'yxatida sanasa — qaror o'shaniki
        // (izohi RequireAnyPermissionAttribute'da). Aks holda class darajasidagi talab uni bekor qilardi.
        if (context.ActionDescriptor.EndpointMetadata.OfType<RequireAnyPermissionAttribute>()
            .Any(any => any.Permissions.Contains(Permission, StringComparer.Ordinal)))
        {
            return;
        }

        ICurrentUser user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (user.HasPermission(Permission))
        {
            return;
        }

        context.Result = AccessResults.Forbidden(context.HttpContext, "Permission denied", null);
    }
}

/// <summary>
/// «Birortasi yetarli» ruxsat talabi — boshqa bo'lim ekrani uchun kerak bo'ladigan O'QISH ro'yxatlariga.
/// </summary>
/// <remarks>
/// F6 stendida (W2) o'lchandi: davomat ekrani xodimlar ro'yxatini (`settings.users`), foydalanuvchilar
/// ekrani rollar ro'yxatini (`settings.roles`), yetkazish yaratish ekrani mijozlar ro'yxatini
/// (`partners.view`) o'qiydi — o'sha ruxsat yo'q operator bo'sh ro'yxat ko'rardi va sababini topolmasdi.
/// Class darajasidagi <see cref="RequirePermissionAttribute"/> shu ro'yxatdagi ruxsat uchun qarorni
/// shu atributga topshiradi; yozish amallari o'z talabini saqlaydi.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireAnyPermissionAttribute : Attribute, IAuthorizationFilter
{
    public RequireAnyPermissionAttribute(params string[] permissions) => Permissions = permissions;

    public IReadOnlyList<string> Permissions { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ICurrentUser user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (Permissions.Any(user.HasPermission))
        {
            return;
        }

        context.Result = AccessResults.Forbidden(context.HttpContext, "Permission denied", null);
    }
}

/// <summary>
/// Modul talabi: tenant Identity obunasida kamida bittasini olgan bo'lsin, aks holda 403
/// <c>module_disabled:KOD</c>. Bir nechta kod — «birortasi» (Counterparties: SUPPLIERS yoki CLIENTS).
/// </summary>
/// <remarks>Angular guard faqat menyuni yashiradi — to'g'ridan-to'g'ri API chaqiruvini shu to'xtatadi.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireModuleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _codes;

    public RequireModuleAttribute(params string[] moduleCodes) => _codes = moduleCodes;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (await AccessResults.TenantStateAsync(context) is not { } state)
        {
            return;
        }

        if (_codes.Any(state.EnabledModules.Contains))
        {
            return;
        }

        string message = Translations.Format(Messages.ModuleDisabled, RequestLanguage.Resolve(context.HttpContext), string.Join(" / ", _codes));
        context.Result = AccessResults.Forbidden(context.HttpContext, message, "module_disabled:" + (_codes.FirstOrDefault() ?? "UNKNOWN"), translate: false);
    }
}

/// <summary>Feature talabi (moduldan bir daraja nozik): o'chiq → 403 <c>feature_disabled:KOD</c>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _codes;

    public RequireFeatureAttribute(params string[] featureCodes) => _codes = featureCodes;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (await AccessResults.TenantStateAsync(context) is not { } state)
        {
            return;
        }

        if (_codes.Any(state.EnabledFeatures.Contains))
        {
            return;
        }

        string message = Translations.Format(Messages.FeatureDisabled, RequestLanguage.Resolve(context.HttpContext), string.Join(" / ", _codes));
        context.Result = AccessResults.Forbidden(context.HttpContext, message, "feature_disabled:" + (_codes.FirstOrDefault() ?? "unknown"), translate: false);
    }
}

/// <summary>
/// Tenant yuzasidagi har controller: tenant konteksti bo'lmasa 403 <c>tenant_missing</c>.
/// </summary>
/// <remarks>
/// Tenantsiz token (masalan tenantsiz platforma admini) bilan RLS har o'qishni 0 qator,
/// har yozishni istisno qilardi — «bo'sh ro'yxat» yolg'on ko'rinish berardi.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RequireTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.RequestServices.GetRequiredService<ICurrentTenant>().IsSet)
        {
            return;
        }

        context.Result = AccessResults.Forbidden(context.HttpContext, "Tenant is required", SubscriptionPolicy.TenantMissing);
    }
}

internal static class AccessResults
{
    public static ObjectResult Forbidden(HttpContext http, string message, string? code, bool translate = true)
    {
        string text = translate ? Translations.Format(message, RequestLanguage.Resolve(http)) : message;
        return new ObjectResult(code is null ? ApiResponse<object>.Fail(text) : ApiResponse<object>.Fail(text, code))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }

    /// <summary>
    /// Tenant holati; <see langword="null"/> — tekshiruv o'tkazib yuborilsin (platforma admini) yoki
    /// natija allaqachon qo'yildi (tenant yo'q).
    /// </summary>
    public static async Task<TenantState?> TenantStateAsync(AuthorizationFilterContext context)
    {
        IServiceProvider services = context.HttpContext.RequestServices;
        if (services.GetRequiredService<ICurrentUser>().IsPlatformAdmin)
        {
            return null;
        }

        if (services.GetRequiredService<ICurrentTenant>().TenantId is not { } tenantId)
        {
            context.Result = Forbidden(context.HttpContext, "Tenant is required", SubscriptionPolicy.TenantMissing);
            return null;
        }

        TenantState? state = await services.GetRequiredService<ITenantStateService>().GetAsync(tenantId, context.HttpContext.RequestAborted);
        if (state is null)
        {
            context.Result = Forbidden(context.HttpContext, Messages.TenantMissing, SubscriptionPolicy.TenantMissing);
        }

        return state;
    }
}
