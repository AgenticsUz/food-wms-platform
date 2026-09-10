using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Identity;
using WMS.API.Surfaces;
using WMS.Application.Interfaces;
using PlatformAdminOptions = Platform.Web.Authorization.PlatformAdminOptions;

namespace WMS.API.Authorization;

/// <summary>
/// JWT claim'lari + <see cref="WmsAccessContext"/> (RBAC bazasi) dan yig'iladigan joriy foydalanuvchi.
/// </summary>
/// <remarks>
/// ⚠️ Prinsipal konstruktorda NUSXALANMAYDI: <c>/admin/v1/*</c> da uni avtorizatsiya bosqichi
/// (Console sxemasi) o'rnatadi va oldinroq olingan nusxa butun so'rov davomida anonim qolardi
/// (Wash'da o'lchangan).
/// </remarks>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>(StringComparer.Ordinal);

    private readonly IHttpContextAccessor _accessor;
    private readonly WmsAccessContext _access;
    private readonly PlatformAdminOptions _admin;
    private readonly ILogger<HttpContextCurrentUser> _logger;
    private readonly Lazy<bool> _isPlatformAdmin;

    public HttpContextCurrentUser(IHttpContextAccessor accessor, WmsAccessContext access, IOptions<PlatformAdminOptions> admin, ILogger<HttpContextCurrentUser> logger)
    {
        _accessor = accessor;
        _access = access;
        _admin = admin.Value;
        _logger = logger;
        _isPlatformAdmin = new Lazy<bool>(ReadPlatformAdmin);
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? Sub => Guid.TryParse(Principal?.FindFirstValue(PlatformClaimNames.Subject), out Guid id) ? id : null;

    public Guid? ProfileId => _access.Access?.ProfileId;

    public string? FullName => _access.Access?.FullName ?? Principal?.FindFirstValue("name");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsPlatformAdmin => _isPlatformAdmin.Value;

    /// <remarks>
    /// Rol ishonch chegarasisiz o'qiladi va bu to'g'ri: bu yerga yetgan prinsipal allaqachon o'z
    /// yuzasining sxemasidan (emitent + audience) o'tgan.
    /// </remarks>
    public bool IsConsoleOperator => IsPlatformAdmin || (Principal?.IsInRole(WmsSurfaces.AdminRole) ?? false);

    public IReadOnlySet<string> Permissions => _access.Access?.Permissions ?? NoPermissions;

    public bool HasPermission(string permission) => IsPlatformAdmin || Permissions.Contains(permission);

    /// <summary>
    /// <c>is_platform_admin</c> — FAQAT sozlangan emitent va WMS audience'laridan biri bilan
    /// kelgan tokenda (fail-closed). Qiymat registrga befarq: OpenIddict <c>true</c>, .NET <c>True</c> yozadi.
    /// </summary>
    private bool ReadPlatformAdmin()
    {
        ClaimsPrincipal? principal = Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        Claim[] claimed = [.. principal.FindAll(PlatformClaimNames.PlatformAdmin)];
        if (claimed.Length == 0)
        {
            return false;
        }

        if (!_admin.IsConfigured
            || !_admin.IsTrustedIssuer(principal.FindFirstValue(PlatformClaimNames.Issuer))
            || !principal.FindAll(PlatformClaimNames.Audience).Any(c => _admin.IsTrustedAudience(c.Value)))
        {
            _logger.LogWarning("is_platform_admin RAD ETILDI: emitent yoki audience ishonchli ro'yxatda emas");
            return false;
        }

        return claimed.Any(c => bool.TryParse(c.Value, out bool value) && value);
    }
}

/// <summary>Console yuzasi (<c>/admin/v1/*</c>) sharti va yordamchilari (§4.4).</summary>
public static class WmsAdminSurface
{
    /// <summary>
    /// Console operatori: <c>is_platform_admin</c> YOKI <see cref="WmsSurfaces.AdminRole"/>.
    /// </summary>
    /// <remarks>
    /// Ikkinchi shart HRM'da F3 da qo'shilgan: usiz «WMS admini» Console roli hech qanday eshik
    /// ochmasdi va uni bergan odam sababini topa olmasdi.
    /// </remarks>
    public static bool IsConsoleOperator(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true
        && (user.FindAll(PlatformClaimNames.PlatformAdmin).Any(c => bool.TryParse(c.Value, out bool v) && v)
            || user.IsInRole(WmsSurfaces.AdminRole));
}

public static class WmsAuthorizationExtensions
{
    public static IServiceCollection AddWmsAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthorization(options => options.AddPolicy(
            WmsSurfaces.AdminPolicy,
            policy => policy
                // ⚠️ Sxema OSHKORA: usiz siyosat standart sxemani olardi va wms-web tokeni admin
                // yuzasida 401 emas, 403 bilan qaytardi — «audience ajratish» mezoni o'lchanmasdi.
                .AddAuthenticationSchemes(WmsSurfaces.AdminScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context => WmsAdminSurface.IsConsoleOperator(context.User))));

        return services;
    }
}
