using Platform.Web.Authentication;
using WMS.API.Surfaces;

namespace WMS.API.Authentication;

/// <summary>
/// Autentifikatsiya: IKKI nomlangan JWT sxemasi (paketniki) va ularni yo'l bo'yicha ajratuvchi
/// standart sxema (Wash naqshi).
/// </summary>
/// <remarks>
/// <para>
/// SQLite davridagi o'z HS256 kaliti (<c>Jwt:Key</c>, 7 kunlik token, <c>sstamp</c>) O'CHDI:
/// tokenni Identity beradi, tekshiruvni paketning <c>AddPlatformJwtBearer</c> i qiladi
/// (emitent slashli/slashsiz, <c>RoleClaimType = roles</c>, ichki JWKS backchannel'i).
/// </para>
/// <para>
/// ⚠️ Nega standart sxema — yo'naltiruvchi. <c>UseAuthentication()</c> faqat standart sxemani
/// yuritadi; u <c>wms-jwt</c> bo'lsa <c>/admin/v1/*</c> so'rovida <c>context.User</c> zanjir
/// oxirigacha anonim qolardi va tenant/huquq middleware'lari Console operatorini ko'rmasdi.
/// Yo'naltirish avtorizatsiya EMAS: chegarani audience va <see cref="WmsSurfaces.AdminPolicy"/> tutadi.
/// </para>
/// </remarks>
public static class WmsAuthenticationExtensions
{
    public const string SurfaceScheme = "wms-surface";

    public static IServiceCollection AddWmsAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string? issuer = configuration[PlatformAuthenticationOptions.AuthorityConfigurationKey];
        string? metadataAddress = configuration[PlatformAuthenticationOptions.MetadataAddressConfigurationKey];

        // Fail-closed, lekin sabab bilan: emitentsiz JWKS yuklanmaydi va HAR token 401 bo'lardi.
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                $"'{PlatformAuthenticationOptions.AuthorityConfigurationKey}' sozlanmagan (Auth__Issuer=https://id.agentics.uz).");
        }

        string apiAudience = Read(configuration, WmsSurfaces.ApiAudienceKey, WmsSurfaces.ApiAudience);
        string adminAudience = Read(configuration, WmsSurfaces.AdminAudienceKey, WmsSurfaces.AdminAudience);

        // Development'da emitent `http://localhost:5245` — HTTPS talabi kalitlarni umuman yuklatmasdi.
        bool requireHttpsMetadata = !environment.IsDevelopment();

        services
            .AddAuthentication(SurfaceScheme)
            .AddPolicyScheme(SurfaceScheme, SurfaceScheme, options =>
                options.ForwardDefaultSelector = context =>
                    WmsSurfaces.IsAdminPath(context.Request.Path) ? WmsSurfaces.AdminScheme : WmsSurfaces.ApiScheme)
            .AddPlatformJwtBearer(WmsSurfaces.ApiScheme, issuer, apiAudience, options =>
            {
                options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = requireHttpsMetadata;

                // WMS'da SignalR hub yo'q — so'rov satridagi tokenga yo'l ochish faqat oshkor bo'lish yo'li.
                options.AllowHubQueryStringToken = false;
            })
            .AddPlatformJwtBearer(WmsSurfaces.AdminScheme, issuer, adminAudience, options =>
            {
                options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.AllowHubQueryStringToken = false;
            });

        return services;
    }

    /// <summary>Bo'sh qiymat sukutni BOSMAYDI (<c>appsettings.json</c> da kalit bo'sh satr bo'lib turishi mumkin).</summary>
    private static string Read(IConfiguration configuration, string key, string fallback)
    {
        string? value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
