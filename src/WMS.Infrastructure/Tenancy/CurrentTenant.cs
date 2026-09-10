using Platform.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Tenancy;

/// <summary>
/// Tenant konteksti — DI SCOPE bo'yicha nusxa (so'rov yoki fon vazifa scope'i).
/// </summary>
/// <remarks>
/// ⚠️ Paketdagi <c>CurrentTenant</c> (<c>AsyncLocal</c>) ATAYLAB ishlatilmaydi (Wash
/// darsi): async metod ichida o'rnatilgan qiymat chaqiruvchiga QAYTMAYDI — JIT sink
/// tenantni qo'yib qaytgach, keyingi qadam 0 qator olardi. Scoped nusxa butun scope'da
/// bir xil. Fon vazifalar har tenant uchun ALOHIDA scope ochadi.
/// </remarks>
public sealed class CurrentTenant : ICurrentTenant
{
    public Guid? TenantId { get; private set; }
    public string? TenantCode { get; private set; }
    public bool IsSet => TenantId is not null;

    public void Set(Guid tenantId, string? tenantCode)
    {
        TenantId = tenantId;
        TenantCode = tenantCode;
    }

    public void Clear()
    {
        TenantId = null;
        TenantCode = null;
    }
}
