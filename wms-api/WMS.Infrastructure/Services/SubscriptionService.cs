using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Subscription;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class SubscriptionService : ISubscriptionService
{
    private readonly WmsDbContext _db;
    private readonly SubscriptionOptions _options;

    public SubscriptionService(WmsDbContext db, IOptions<SubscriptionOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<SubscriptionInfoDto> GetForTenantAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");

        var plan = tenant.PlanId != null
            ? await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == tenant.PlanId, ct)
            : null;

        var modules = await _db.TenantModules.AsNoTracking()
            .Where(tm => tm.TenantId == tenantId && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var state = new TenantState
        {
            Id = tenant.Id, Name = tenant.Name, Slug = tenant.Slug,
            IsActive = tenant.IsActive, Status = tenant.SubscriptionStatus,
            TrialEndsAt = tenant.TrialEndsAt, PlanId = tenant.PlanId,
            EnabledModules = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase)
        };
        var verdict = SubscriptionPolicy.Evaluate(state, _options, now);
        var daysLeft = SubscriptionPolicy.TrialDaysLeft(state, now);

        return new SubscriptionInfoDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Slug = tenant.Slug,
            PlanId = plan?.Id,
            PlanName = plan?.Name,
            PlanCode = plan?.Code,
            PlanPrice = plan?.Price ?? 0,
            Status = tenant.SubscriptionStatus,
            IsActive = tenant.IsActive,
            TrialEndsAt = tenant.TrialEndsAt,
            TrialDaysLeft = daysLeft,
            GraceDays = _options.GraceDays,
            IsExpiringSoon = daysLeft is { } d && d <= _options.WarnBeforeDays,
            IsBlocked = !verdict.Allowed,
            BlockedReason = verdict.Code,
            EnabledModules = modules,
            Limits = await PlanLimits.GetUsageAsync(_db, tenantId, plan, ct),
            LimitWarnPercent = _options.LimitWarnPercent
        };
    }

    public async Task<List<PlanDto>> GetAvailablePlansAsync(CancellationToken ct = default)
    {
        var plans = await _db.Plans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync(ct);

        return plans.Select(p => new PlanDto
        {
            Id = p.Id, Name = p.Name, Code = p.Code, Price = p.Price, IsActive = p.IsActive,
            ModuleCodes = PlanModules.Split(p.ModuleCodes),
            MaxUsers = p.MaxUsers, MaxWarehouses = p.MaxWarehouses,
            MaxTransfersPerMonth = p.MaxTransfersPerMonth
        }).ToList();
    }
}
