using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
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
/// 4. Muddati `WarnBeforeDays` ichida tugaydiganlar → adminlarga bildirishnoma (TG5, R6);
///    to'xtatilganlar ham bildirishnoma oladi — ilgari mijoz buni faqat kirganda banner'da ko'rardi.
///
/// Ma'lumot HECH QACHON o'chirilmaydi. Kirish baribir har so'rovda tekshiriladi
/// (SubscriptionPolicy) — bu fon ishi holatni control plane'da to'g'ri ko'rsatish uchun.
/// </summary>
/// <remarks>
/// F6 (D12): 1–3 qadamlar uchun <c>TenantScopes.ForEachTenantAsync</c> ATAYLAB ishlatilmaydi — ular FAQAT
/// <c>tenant</c> jadvaliga tegadi va u platforma jadvali (RLS yo'q, <c>ITenantEntity</c> emas). Bitta
/// scope'dagi bitta o'tish yetarli va to'g'ri; <c>ForEachTenantAsync</c> faqat <c>is_active</c> tenantlarni
/// aylanadi, bu yerda esa o'chirilgan tenantning muddatli to'xtatishi ham to'g'ri yopilishi kerak.
/// 4-qadam esa bildirishnoma (tenant jadvali, RLS) yozadi — FAQAT tegishli tenantlar uchun alohida scope.
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

    /// <summary>Tenantga yuboriladigan bildirishnoma (4-qadam), tenant scope'idan tashqarida yig'iladi.</summary>
    private sealed record PendingNotice(Guid TenantId, string Code, string Title, string Template, string?[] Args, NotificationType Type);

    private async Task RunAsync(CancellationToken ct)
    {
        List<PendingNotice> notices = [];
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
                notices.Add(Suspended(tenant));
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
                notices.Add(Suspended(tenant));
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

            // 4. Muddati yaqin (WarnBeforeDays ichida, hali tugamagan) — faol tenantlar.
            var warnUntil = now.AddDays(_options.WarnBeforeDays);
            var expiring = await db.Tenants.AsNoTracking()
                .Where(t => t.IsActive && (
                    (t.SubscriptionStatus == SubscriptionStatus.Trial && t.TrialEndsAt != null && t.TrialEndsAt >= now && t.TrialEndsAt <= warnUntil)
                    || (t.SubscriptionStatus == SubscriptionStatus.Active && t.PaidUntil != null && t.PaidUntil >= now && t.PaidUntil <= warnUntil)))
                .Select(t => new { t.Id, t.Code, t.SubscriptionStatus, t.TrialEndsAt, t.PaidUntil })
                .ToListAsync(ct);

            foreach (var t in expiring)
            {
                bool trial = t.SubscriptionStatus == SubscriptionStatus.Trial;
                DateTime deadline = (trial ? t.TrialEndsAt : t.PaidUntil)!.Value;
                notices.Add(new PendingNotice(t.Id, t.Code, NotificationMessages.SubscriptionExpiringTitle,
                    trial ? NotificationMessages.TrialEnding : NotificationMessages.PaidEnding,
                    [NotificationMessages.Date(deadline)], NotificationType.SubscriptionWarning));
            }

            if (touched.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                foreach (var id in touched) state.Invalidate(id);

                _logger.LogInformation(
                    "Obuna sikli: {Trials} trial tugadi, {Unpaid} to'lanmagan to'xtatildi, {Back} qayta yoqildi",
                    expiredTrials.Count, unpaid.Count, dueForReactivation.Count);
            }
        }
#pragma warning disable CA1031 // Kunlik ish yiqilsa xizmat to'xtamasin — ertaga qayta urinadi.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Obuna siklini tekshirish yiqildi");
            return;
        }

        foreach (PendingNotice notice in notices)
            await NotifyTenantAsync(notice, ct);
    }

    private static PendingNotice Suspended(Tenant tenant) =>
        new(tenant.Id, tenant.Code, NotificationMessages.SubscriptionSuspendedTitle,
            NotificationMessages.SuspendedNonPayment, [], NotificationType.SubscriptionSuspended);

    /// <summary>
    /// Tenant scope'ida bildirishnoma. Dedupe — o'sha shablon va argumentlar (muddat sanasi) bilan
    /// allaqachon bor bo'lsa yozilmaydi: sana uzaytirilsa (to'lov) yangi sana — yangi xabar.
    /// To'xtatish — bir sikl ichida (24 soat) bittadan ko'p emas.
    /// </summary>
    private async Task NotifyTenantAsync(PendingNotice notice, CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(notice.TenantId, notice.Code);
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

            string argsJson = System.Text.Json.JsonSerializer.Serialize(notice.Args);
            DateTime since = DateTime.UtcNow.AddHours(-23);
            bool already = await db.Notifications.AnyAsync(n => n.Type == notice.Type && n.MessageTemplate == notice.Template
                && (notice.Type == NotificationType.SubscriptionWarning ? n.MessageArgs == argsJson : n.CreatedAt >= since), ct);
            if (already) return;

            await scope.ServiceProvider.GetRequiredService<INotificationService>()
                .NotifyAsync(null, notice.Title, notice.Template, notice.Args, notice.Type, "Tenant", notice.TenantId);

            // Platforma egasiga ham (TG17) — kimga qo'ng'iroq qilishni bilsin.
            string what = notice.Type == NotificationType.SubscriptionSuspended
                ? "🚫 to'xtatildi (to'lov)"
                : $"💳 muddati {notice.Args.FirstOrDefault()} da tugaydi";
            await scope.ServiceProvider.GetRequiredService<IOpsNotifier>().SendAsync(
                $"Tenant <b>{System.Net.WebUtility.HtmlEncode(notice.Code)}</b>: {what}",
                $"ops:sub:{notice.Type}:{notice.TenantId:N}:{string.Join('|', notice.Args)}", ct);
        }
#pragma warning disable CA1031 // Bitta tenantning bildirishnomasi qolganlarini to'xtatmasin.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Obuna bildirishnomasi tenant {TenantCode} uchun yozilmadi", notice.Code);
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
