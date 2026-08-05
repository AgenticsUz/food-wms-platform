using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
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
    private readonly IConfiguration _config;
    private readonly IRequestLanguage _language;

    public SubscriptionService(WmsDbContext db, IOptions<SubscriptionOptions> options,
        IConfiguration config, IRequestLanguage language)
    {
        _db = db;
        _options = options.Value;
        _config = config;
        _language = language;
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
        var moduleSet = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);

        var features = await FeatureResolver.ResolveAsync(_db, tenantId, moduleSet, ct);

        var now = DateTime.UtcNow;
        var state = new TenantState
        {
            Id = tenant.Id, Name = tenant.Name, Slug = tenant.Slug,
            IsActive = tenant.IsActive, Status = tenant.SubscriptionStatus,
            TrialEndsAt = tenant.TrialEndsAt, PlanId = tenant.PlanId,
            PaidUntil = tenant.PaidUntil,
            SuspendReason = tenant.SuspendReason,
            SuspendPublicMessage = tenant.SuspendPublicMessage,
            SuspendedUntil = tenant.SuspendedUntil,
            EnabledModules = moduleSet
        };
        var verdict = SubscriptionPolicy.Evaluate(state, _options, now);

        var trialDaysLeft = SubscriptionPolicy.TrialDaysLeft(state, now);
        var paidDaysLeft = SubscriptionPolicy.PaidDaysLeft(tenant.PaidUntil, now);
        var soon = (trialDaysLeft is { } t && t <= _options.WarnBeforeDays)
                   || (paidDaysLeft is { } p && p <= _options.WarnBeforeDays);

        return new SubscriptionInfoDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            PlanName = plan?.Name,
            PlanCode = plan?.Code,
            PlanPrice = plan?.Price ?? 0,
            Status = tenant.SubscriptionStatus.ToString(),

            TrialEndsAt = tenant.TrialEndsAt,
            DaysUntilTrialEnd = trialDaysLeft,

            PaidUntil = tenant.PaidUntil,
            DaysUntilPaidEnd = paidDaysLeft,
            PaymentGraceDays = _options.PaidGraceDays,

            IsBlocked = !verdict.Allowed,
            BlockedReason = verdict.Code,
            // The operator's own wording wins over the generic text — that is why they wrote it.
            // Only the generic text is translated; a message typed by a human is left alone.
            BlockedMessage = verdict.Allowed
                ? null
                : (verdict.PublicMessage ?? Translations.Format(verdict.Message, _language.Current)),
            SuspendedUntil = tenant.SuspendedUntil,

            IsExpiringSoon = soon,
            WarnBeforeDays = _options.WarnBeforeDays,
            LimitWarnPercent = _options.LimitWarnPercent,

            Limits = await PlanLimits.GetLimitsAsync(_db, tenantId, plan, _options.LimitWarnPercent, ct),

            EnabledModules = modules,
            EnabledFeatures = features.Where(f => f.IsEnabled).Select(f => f.Code).ToList(),

            SupportPhone = _config["Support:Phone"],
            SupportEmail = _config["Support:Email"],

            Branding = new WMS.Application.DTOs.Branding.BrandingDto
            {
                LogoUrl = tenant.LogoUrl,
                LogoSquareUrl = tenant.LogoSquareUrl,
                BrandColor = tenant.BrandColor
            }
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
            FeatureCodes = PlanModules.Split(p.FeatureCodes),
            MaxUsers = p.MaxUsers, MaxWarehouses = p.MaxWarehouses,
            MaxTransfersPerMonth = p.MaxTransfersPerMonth,
            TrialDays = p.TrialDays, IsDefault = p.IsDefault
        }).ToList();
    }
}
