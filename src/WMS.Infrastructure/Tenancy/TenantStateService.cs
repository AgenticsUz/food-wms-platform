using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Tenancy;

/// <inheritdoc />
public sealed class TenantStateService : ITenantStateService
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

    private static string Key(Guid tenantId) => $"tenant-state:{tenantId:N}";

    public async Task<TenantState?> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
        {
            return null;
        }

        if (_cache.TryGetValue(Key(tenantId), out TenantState? cached))
        {
            return cached;
        }

        // `tenant` — platforma jadvali (RLS yo'q). Nusxa hali yozilmagan bo'lsa (JIT
        // yiqilgan) holat null va siyosat uni «tenant yo'q» deb baholaydi.
        var tenant = await _db.Tenants.AsNoTracking().Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        TenantState? state = null;
        if (tenant is not null)
        {
            HashSet<string> modules = new(tenant.ModuleCodes, StringComparer.OrdinalIgnoreCase);
            var features = await FeatureResolver.ResolveAsync(_db, tenantId, tenant.PlanId, modules, ct);

            state = new TenantState
            {
                Id = tenant.Id,
                Code = tenant.Code,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                Status = tenant.SubscriptionStatus,
                TrialEndsAt = tenant.TrialEndsAt,
                PlanId = tenant.PlanId,
                PlanCode = tenant.Plan?.Code,
                PlanName = tenant.Plan?.Name,
                PaidUntil = tenant.PaidUntil,
                SuspendReason = tenant.SuspendReason,
                SuspendPublicMessage = tenant.SuspendPublicMessage,
                SuspendedUntil = tenant.SuspendedUntil,
                LogoUrl = tenant.LogoUrl,
                LogoSquareUrl = tenant.LogoSquareUrl,
                BrandColor = tenant.BrandColor,
                EnabledModules = modules,
                EnabledFeatures = new HashSet<string>(features.Where(f => f.IsEnabled).Select(f => f.Code), StringComparer.OrdinalIgnoreCase),
            };
        }

        // Yo'q tenant ham qisqa muddat keshlanadi — buzuq token bazani bosib yubormasin.
        _cache.Set(Key(tenantId), state, TimeSpan.FromSeconds(Math.Max(5, _options.StateCacheSeconds)));
        return state;
    }

    public void Invalidate(Guid tenantId) => _cache.Remove(Key(tenantId));
}
