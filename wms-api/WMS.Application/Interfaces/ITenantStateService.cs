using WMS.Application.Common;

namespace WMS.Application.Interfaces;

/// Short-lived cache over the tenant's subscription state + enabled modules.
/// Every authenticated request consults it, so it must not hit the DB every time;
/// control-plane writes (suspend/activate/plan/module changes) call <see cref="Invalidate"/>
/// so the change is visible immediately instead of after the cache window.
public interface ITenantStateService
{
    Task<TenantState?> GetAsync(int tenantId, CancellationToken ct = default);
    void Invalidate(int tenantId);
}
