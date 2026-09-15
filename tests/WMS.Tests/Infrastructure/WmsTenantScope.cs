using Microsoft.Extensions.DependencyInjection;
using Platform.Infrastructure.Tenancy;
using WMS.Infrastructure.Persistence;

namespace WMS.Tests.Infrastructure;

/// <summary>
/// Tenant konteksti O'RNATILGAN DI qamrovi: ichidan olingan har servis xuddi
/// so'rov qamrovidagidek ishlaydi.
/// </summary>
/// <remarks>
/// ⚠️ Tenant qamrov ochilishi bilanoq, BIRINCHI baza so'rovidan oldin qo'yiladi:
/// <c>TenantConnectionInterceptor</c> <c>app.tenant_id</c> ni ULANISH OCHILGAN
/// lahzada o'qiydi, keyin qo'yilgan qiymat o'sha ulanishga yetib bormaydi va
/// RLS jimgina 0 qator qaytarardi.
/// </remarks>
public sealed class WmsTenantScope : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;

    /// <summary>Qamrov ochadi va tenantni o'rnatadi.</summary>
    /// <param name="services">Ildiz provayder.</param>
    /// <param name="tenant">Tenant.</param>
    public WmsTenantScope(IServiceProvider services, TestTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(tenant);

        _scope = services.CreateAsyncScope();
        _scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(tenant.Id, tenant.Code);
    }

    /// <summary>Shu qamrovdagi kontekst.</summary>
    public WmsDbContext Db => Service<WmsDbContext>();

    /// <summary>Qamrovdagi «joriy foydalanuvchi»ni va uning ruxsatlarini o'rnatadi.</summary>
    /// <param name="profileId"><c>user_profile.id</c> (<c>TestData.AddUserAsync</c> dan).</param>
    /// <param name="permissions">Ruxsat kodlari — faqat KERAKLILARI berilsin.</param>
    /// <returns>O'sha qamrov (zanjirlab yozish uchun).</returns>
    /// <remarks>
    /// Ruxsatga bog'liq xatti-harakat (masalan orqaga sana) servisda tekshiriladi, chunki
    /// entitlement YUK TARKIBIGA bog'liq — atribut buni ko'ra olmaydi.
    /// </remarks>
    public WmsTenantScope AsUser(Guid? profileId, params string[] permissions)
    {
        Service<TestCurrentUser>().Set(profileId, permissions);
        return this;
    }

    /// <summary>Qamrovdan servis oladi.</summary>
    /// <typeparam name="T">Servis turi.</typeparam>
    /// <returns>Servis nusxasi.</returns>
    public T Service<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}
