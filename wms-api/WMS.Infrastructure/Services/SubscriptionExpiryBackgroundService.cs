using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>
/// Kuniga bir marta obuna hayotiy siklini yuritadi:
///
/// 1. Muddati (trial + grace) o'tgan demo tenantlar → Suspended.
/// 2. To'lov muddati (PaidUntil + grace) o'tgan tenantlar → Suspended, sabab NonPayment.
/// 3. `SuspendedUntil` sanasi kelgan vaqtincha to'xtatishlar → avtomatik Active
///    (to'lovi ham o'tgan bo'lsa — yoqilmaydi, NonPayment bilan to'xtab qoladi).
///
/// Ma'lumot HECH QACHON o'chirilmaydi. Kirish baribir har so'rovda tekshiriladi
/// (SubscriptionPolicy) — bu fon ishi holatni control plane'da to'g'ri ko'rsatish uchun.
/// </summary>
public class SubscriptionExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpiryBackgroundService> _logger;
    private readonly SubscriptionOptions _options;

    public SubscriptionExpiryBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpiryBackgroundService> logger, IOptions<SubscriptionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            var state = scope.ServiceProvider.GetRequiredService<ITenantStateService>();

            var now = DateTime.UtcNow;
            var touched = new HashSet<int>();

            // 1. Trial tugadi
            var trialCutoff = now.AddDays(-_options.GraceDays);
            var expiredTrials = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Trial
                            && t.TrialEndsAt != null && t.TrialEndsAt < trialCutoff)
                .ToListAsync(ct);

            foreach (var tenant in expiredTrials)
            {
                tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
                tenant.SuspendReason = SuspendReason.NonPayment;
                tenant.SuspendedAt = now;
                tenant.SuspendedUntil = null;
                touched.Add(tenant.Id);
            }

            // 2. To'lov muddati tugadi
            var paidCutoff = now.AddDays(-_options.PaidGraceDays);
            var unpaid = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Active
                            && t.PlanId != null
                            && t.PaidUntil != null && t.PaidUntil < paidCutoff)
                .ToListAsync(ct);

            foreach (var tenant in unpaid)
            {
                tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
                tenant.SuspendReason = SuspendReason.NonPayment;
                tenant.SuspendedAt = now;
                tenant.SuspendedUntil = null;
                touched.Add(tenant.Id);
            }

            // 3. Muddatli to'xtatish tugadi → o'zi yoqiladi
            var dueForReactivation = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Suspended
                            && t.SuspendedUntil != null && t.SuspendedUntil <= now)
                .ToListAsync(ct);

            foreach (var tenant in dueForReactivation)
            {
                // To'lovi o'tib ketgan bo'lsa qayta yoqmaymiz — aks holda "2 oyga to'xtating"
                // deb so'ragan mijoz to'lamasdan turib o'zi yoqilib qolardi.
                if (tenant.PaidUntil is { } paidUntil && paidUntil.AddDays(_options.PaidGraceDays) < now)
                {
                    tenant.SuspendReason = SuspendReason.NonPayment;
                    tenant.SuspendedUntil = null;
                    touched.Add(tenant.Id);
                    continue;
                }

                tenant.SubscriptionStatus = SubscriptionStatus.Active;
                tenant.IsActive = true;
                TenantService.ClearSuspension(tenant);
                touched.Add(tenant.Id);
            }

            if (touched.Count == 0) return;

            await db.SaveChangesAsync(ct);
            foreach (var id in touched) state.Invalidate(id);

            _logger.LogInformation(
                "Subscription lifecycle: {Trials} trial(s) expired, {Unpaid} unpaid suspended, {Back} reactivated",
                expiredTrials.Count, unpaid.Count, dueForReactivation.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription lifecycle check failed");
        }
    }
}
