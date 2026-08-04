using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class TenantStateService : ITenantStateService
{
    private readonly WmsDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly SubscriptionOptions _options;

    public TenantStateService(WmsDbContext db, IMemoryCache cache, IOptions<SubscriptionOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    private static string Key(int tenantId) => $"tenant-state:{tenantId}";

    public async Task<TenantState?> GetAsync(int tenantId, CancellationToken ct = default)
    {
        if (tenantId <= 0) return null;
        if (_cache.TryGetValue<TenantState>(Key(tenantId), out var cached)) return cached;

        // Soft-deleted tenants are filtered out globally, so a deleted tenant reads as null
        // and the policy treats it as "no longer exists".
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        TenantState? state = null;
        if (tenant != null)
        {
            var modules = await _db.TenantModules
                .AsNoTracking()
                .Where(tm => tm.TenantId == tenantId && tm.IsEnabled)
                .Select(tm => tm.Module.Code)
                .ToListAsync(ct);
            var moduleSet = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);

            // Features are resolved and cached together with modules, so a per-request
            // feature check costs nothing extra.
            var features = await FeatureResolver.ResolveAsync(_db, tenantId, moduleSet, ct);

            state = new TenantState
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                IsActive = tenant.IsActive,
                Status = tenant.SubscriptionStatus,
                TrialEndsAt = tenant.TrialEndsAt,
                PlanId = tenant.PlanId,
                PaidUntil = tenant.PaidUntil,
                SuspendReason = tenant.SuspendReason,
                SuspendPublicMessage = tenant.SuspendPublicMessage,
                SuspendedUntil = tenant.SuspendedUntil,
                EnabledModules = moduleSet,
                EnabledFeatures = new HashSet<string>(
                    features.Where(f => f.IsEnabled).Select(f => f.Code), StringComparer.OrdinalIgnoreCase)
            };
        }

        // Missing tenants are cached too (briefly), so a bad token cannot hammer the DB.
        _cache.Set(Key(tenantId), state,
            TimeSpan.FromSeconds(Math.Max(5, _options.StateCacheSeconds)));
        return state;
    }

    public void Invalidate(int tenantId) => _cache.Remove(Key(tenantId));
}
