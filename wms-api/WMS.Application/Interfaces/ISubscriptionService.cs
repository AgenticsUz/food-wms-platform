using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Subscription;

namespace WMS.Application.Interfaces;

/// Tenant-facing view of its own subscription (the control plane's read side).
public interface ISubscriptionService
{
    Task<SubscriptionInfoDto> GetForTenantAsync(int tenantId, CancellationToken ct = default);

    /// Active plans, so the client can show what an upgrade would give it.
    Task<List<PlanDto>> GetAvailablePlansAsync(CancellationToken ct = default);
}
