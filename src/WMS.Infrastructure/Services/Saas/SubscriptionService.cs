using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Subscription;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;
using TenantState = WMS.Application.Common.TenantState;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
public class SubscriptionService : ISubscriptionService
{
    private readonly WmsDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly SubscriptionOptions _options;
    private readonly IConfiguration _config;
    private readonly IRequestLanguage _language;

    public SubscriptionService(WmsDbContext db, ICurrentTenant currentTenant, IOptions<SubscriptionOptions> options,
        IConfiguration config, IRequestLanguage language)
    {
        _db = db;
        _currentTenant = currentTenant;
        _options = options.Value;
        _config = config;
        _language = language;
    }

    public async Task<SubscriptionInfoDto> GetForTenantAsync(CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? throw new ForbiddenException("Tenant is required");

        // Keshdan EMAS, bazadan: bloklangan mijoz shu ekranni «qayta yuklash» bilan tekshiradi va
        // Console'da to'lov kiritilgan zahoti ochilganini ko'rishi kerak.
        var tenant = await _db.Tenants.AsNoTracking().Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");
        var plan = tenant.Plan;

        // Modullar Identity obunasidan (nusxa), SQLite davridagi TenantModule jadvalidan emas (D6).
        var moduleSet = new HashSet<string>(tenant.ModuleCodes, StringComparer.OrdinalIgnoreCase);
        var features = await FeatureResolver.ResolveAsync(_db, tenantId, tenant.PlanId, moduleSet, ct);

        var now = DateTime.UtcNow;
        var state = new TenantState
        {
            Id = tenant.Id, Code = tenant.Code, Name = tenant.Name,
            IsActive = tenant.IsActive, Status = tenant.SubscriptionStatus,
            TrialEndsAt = tenant.TrialEndsAt, PlanId = tenant.PlanId,
            PaidUntil = tenant.PaidUntil,
            SuspendReason = tenant.SuspendReason,
            SuspendPublicMessage = tenant.SuspendPublicMessage,
            SuspendedUntil = tenant.SuspendedUntil,
            EnabledModules = moduleSet
        };
        var verdict = SubscriptionPolicy.Evaluate(state, _options, now);
        // Faqat blok haqiqatan olib tashlangan bo'lsa. To'lov muddati o'tib ketgan mijoz
        // muddatli to'xtatish tugagach ham ishlay olmaydi (fon xizmati uni NonPayment ga
        // o'tkazadi) — unga "Faol" deb ko'rsatish yolg'on bo'lardi.
        var suspensionElapsed = tenant.SubscriptionStatus == SubscriptionStatus.Suspended
            && SubscriptionPolicy.IsSuspensionElapsed(state, now)
            && verdict.Allowed;

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
            // Muddati kelgan to'xtatish endi bloklamaydi (SubscriptionPolicy) — holat ham
            // shunga mos kelsin, aks holda mijoz ishlay olsa ham "To'xtatilgan" deb turadi.
            Status = suspensionElapsed
                ? SubscriptionStatus.Active.ToString()
                : tenant.SubscriptionStatus.ToString(),

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
            SuspendedUntil = suspensionElapsed ? null : tenant.SuspendedUntil,

            IsExpiringSoon = soon,
            WarnBeforeDays = _options.WarnBeforeDays,
            LimitWarnPercent = _options.LimitWarnPercent,

            Limits = await PlanLimits.GetLimitsAsync(_db, plan, _options.LimitWarnPercent, ct),

            EnabledModules = [.. moduleSet.Order(StringComparer.Ordinal)],
            EnabledFeatures = features.Where(f => f.IsEnabled).Select(f => f.Code).ToList(),

            SupportPhone = _config["Support:Phone"],
            SupportEmail = _config["Support:Email"]
        };
    }

    public async Task<List<PlanDto>> GetAvailablePlansAsync(CancellationToken ct = default)
    {
        var plans = await _db.Plans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync(ct);

        // Boshqa tenantlar soni tenantga ko'rsatilmaydi — 0.
        return plans.Select(p => PlanService.MapToDto(p, 0)).ToList();
    }
}
