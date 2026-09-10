using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Saas;

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
/// <remarks>
/// F6 (D12): <c>TenantScopes.ForEachTenantAsync</c> ATAYLAB ishlatilmaydi — bu ish FAQAT <c>tenant</c>
/// jadvaliga tegadi va u platforma jadvali (RLS yo'q, <c>ITenantEntity</c> emas). Bitta scope'dagi bitta
/// o'tish yetarli va to'g'ri; har tenantga alohida scope ochish N ta bir qatorli so'rov bo'lardi.
/// Bundan tashqari <c>ForEachTenantAsync</c> faqat <c>is_active</c> tenantlarni aylanadi, bu yerda esa
/// o'chirilgan tenantning muddatli to'xtatishi ham to'g'ri yopilishi kerak. Kesh bekor qilish
/// (<c>ITenantStateService.Invalidate</c>) — faqat xotira, kontekst talab qilmaydi.
/// </remarks>
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
        // Startupdan keyin kutadi: migrator va birinchi so'rovlar bilan ulanish uchun talashmasin.
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            var state = scope.ServiceProvider.GetRequiredService<ITenantStateService>();

            var now = DateTime.UtcNow;
            var touched = new HashSet<Guid>();

            // 1. Trial tugadi
            var trialCutoff = now.AddDays(-_options.GraceDays);
            var expiredTrials = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Trial
                            && t.TrialEndsAt != null && t.TrialEndsAt < trialCutoff)
                .ToListAsync(ct);

            foreach (var tenant in expiredTrials)
            {
                SuspendForNonPayment(tenant, now);
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
                SuspendForNonPayment(tenant, now);
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
                TenantService.ClearSuspension(tenant);
                touched.Add(tenant.Id);
            }

            if (touched.Count == 0) return;

            await db.SaveChangesAsync(ct);
            foreach (var id in touched) state.Invalidate(id);

            _logger.LogInformation(
                "Obuna sikli: {Trials} trial tugadi, {Unpaid} to'lanmagan to'xtatildi, {Back} qayta yoqildi",
                expiredTrials.Count, unpaid.Count, dueForReactivation.Count);
        }
#pragma warning disable CA1031 // Kunlik ish yiqilsa xizmat to'xtamasin — ertaga qayta urinadi.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Obuna siklini tekshirish yiqildi");
        }
    }

    /// Avtomatik to'xtatish — operator emas (<c>SuspendedBySub</c> bo'sh), shuning uchun kartada
    /// «kim to'xtatdi» maydoni tizim ekanini ko'rsatadi.
    private static void SuspendForNonPayment(Tenant tenant, DateTime now)
    {
        tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
        tenant.SuspendReason = SuspendReason.NonPayment;
        tenant.SuspendedAt = now;
        tenant.SuspendedUntil = null;
        tenant.SuspendedBySub = null;
    }
}
