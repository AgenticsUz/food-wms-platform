using WMS.Application.Common;

namespace WMS.Application.Interfaces;

/// <summary>
/// Tenantning obuna holati, modullari va feature'lari ustidagi qisqa kesh.
/// </summary>
/// <remarks>
/// Har autentifikatsiyali so'rov unga murojaat qiladi, shuning uchun har safar bazaga
/// bormasligi kerak. Control plane yozuvlari (suspend, plan, feature, to'lov) va JIT
/// (modul o'zgarganda) <see cref="Invalidate"/> ni chaqiradi — o'zgarish kesh oynasini
/// kutmay ko'rinsin.
/// <para>
/// ⚠️ Feature override'lari (<c>tenant_feature</c>) RLS ostida: holat JORIY tenant
/// kontekstida yig'iladi. Boshqa tenantni so'rash kerak bo'lsa (Console, fon vazifa)
/// avval kontekstni o'sha tenantga qo'ying.
/// </para>
/// </remarks>
public interface ITenantStateService
{
    Task<TenantState?> GetAsync(Guid tenantId, CancellationToken ct = default);
    void Invalidate(Guid tenantId);
}
