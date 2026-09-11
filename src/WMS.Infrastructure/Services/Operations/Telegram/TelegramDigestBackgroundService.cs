using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Analytics;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Kunlik xulosa (TG11): har tenantda <c>DigestHour</c> (Toshkent) da <c>dashboard.view</c> + xulosani
/// yoqqan ulanishlarga bitta xabar — kutilayotgan tasdiqlar, faol buyurtmalar, kam zaxira, muddati
/// yaqin partiyalar, qarz. Hech narsa bo'lmagan kun — yuborilmaydi. Bir soat keyin platforma egasiga
/// kunlik qator (TG17): faol tenantlar, kechagi transferlar, navbat holati.
/// </summary>
/// <remarks>
/// Daqiqalik tekshiruv + «bugun bajarildi» sanasi: cron yo'q (D12), restartdan keyin ham bir kunda
/// ikki marta ketmaydi — navbat <c>dedup_key</c> ham (<c>digest:{sana}:{chat}</c>) shuni kafolatlaydi.
/// </remarks>
public sealed class TelegramDigestBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);
    private const int TopDebtors = 5;
    private const int ExpiringDays = 7;

    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramDigestBackgroundService> _logger;
    private DateOnly? _lastDigestDate;
    private DateOnly? _lastOpsDate;

    public TelegramDigestBackgroundService(IServiceProvider services, ITelegramService telegram,
        IOptions<TelegramOptions> options, ILogger<TelegramDigestBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _services = services;
        _telegram = telegram;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_telegram.IsEnabled) return;
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now = DateTime.UtcNow;
            try
            {
                if (TelegramQuietHours.IsDue(now, _options.DigestHour, _lastDigestDate))
                {
                    _lastDigestDate = DateOnly.FromDateTime(TelegramQuietHours.ToTashkent(now));
                    await RunDigestAsync(stoppingToken);
                }

                if (TelegramQuietHours.IsDue(now, _options.DigestHour + 1, _lastOpsDate))
                {
                    _lastOpsDate = DateOnly.FromDateTime(TelegramQuietHours.ToTashkent(now));
                    await RunOpsLineAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Kunlik ish yiqilsa xizmat to'xtamasin.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Telegram kunlik xulosa yiqildi");
            }

            await Task.Delay(Tick, stoppingToken);
        }
    }

    private async Task RunDigestAsync(CancellationToken ct)
    {
        int sent = 0;
        await TenantScopes.ForEachTenantAsync(_services, async (scope, tenantId, token) =>
        {
            WmsDbContext db = scope.GetRequiredService<WmsDbContext>();

            var links = await db.TelegramLinks.AsNoTracking()
                .Where(l => l.IsActive && l.Digest && l.UserProfileId != null && l.UserProfile!.IsActive
                    && db.UserRoles.Any(ur => ur.UserId == l.UserProfileId
                        && ur.Role.RolePermissions.Any(rp => rp.PermissionCode == WmsPermissions.DashboardView)))
                .Select(l => new { l.Id, l.ChatId, l.Lang })
                .ToListAsync(token);
            if (links.Count == 0) return;

            IAnalyticsService analytics = scope.GetRequiredService<IAnalyticsService>();
            DashboardSummaryDto summary = await analytics.GetDashboardSummary();
            List<TopDebtorDto> debtors = await analytics.GetTopDebtors(TopDebtors);

            DateTime cutoff = DateTime.UtcNow.Date.AddDays(ExpiringDays + 1);
            int expiring = await db.Batches.CountAsync(b => b.RemainingQuantity > 0 && b.ExpiryDate != null && b.ExpiryDate < cutoff, token);

            // Bo'sh kun — yuborilmaydi: har kuni «hammasi nol» xabari shovqin.
            if (summary.PendingTransfers == 0 && summary.ActiveProductionOrders == 0
                && summary.LowStockProductCount == 0 && expiring == 0)
                return;

            string tenantName = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.Name).FirstAsync(token);
            string dateKey = TelegramQuietHours.ToTashkent(DateTime.UtcNow).ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            foreach (var link in links.DistinctBy(l => l.ChatId))
            {
                string text = Compose(tenantName, summary, debtors, expiring, link.Lang);
                db.TelegramOutboxes.Add(new TelegramOutbox
                {
                    TenantId = tenantId,
                    TelegramLinkId = link.Id,
                    ChatId = link.ChatId,
                    Text = text,
                    DedupKey = $"digest:{dateKey}:{link.ChatId.ToString(CultureInfo.InvariantCulture)}",
                });
                sent++;
            }

            await db.SaveChangesAsync(token);
        }, _logger, ct);

        if (sent > 0) _logger.LogInformation("Telegram kunlik xulosa: {Count} xabar navbatga yozildi", sent);
    }

    private static string Compose(string tenantName, DashboardSummaryDto s, List<TopDebtorDto> debtors, int expiring, string lang)
    {
        string date = TelegramQuietHours.ToTashkent(DateTime.UtcNow).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var sb = new System.Text.StringBuilder();
        sb.Append("🏭 <b>").Append(WebUtility.HtmlEncode(tenantName)).Append("</b>\n");
        sb.Append("📋 <b>").Append(WebUtility.HtmlEncode(Translations.Format(DigestKeys.Title, lang, date))).Append("</b>\n");
        sb.Append("🕓 ").Append(Translations.Format(DigestKeys.Pending, lang, s.PendingTransfers)).Append('\n');
        sb.Append("🏭 ").Append(Translations.Format(DigestKeys.Production, lang, s.ActiveProductionOrders)).Append('\n');
        sb.Append("⚠️ ").Append(Translations.Format(DigestKeys.LowStock, lang, s.LowStockProductCount)).Append('\n');
        sb.Append("⏳ ").Append(Translations.Format(DigestKeys.Expiring, lang, ExpiringDays, expiring)).Append('\n');
        sb.Append("💰 ").Append(Translations.Format(DigestKeys.Debt, lang, NotificationMessages.Amount(s.TotalDebt)));

        if (debtors.Count > 0)
        {
            sb.Append("\n\n").Append(Translations.Format(DigestKeys.TopDebtors, lang));
            foreach (TopDebtorDto d in debtors)
                sb.Append("\n• ").Append(WebUtility.HtmlEncode(d.CounterpartyName)).Append(" — ").Append(NotificationMessages.Amount(d.DebtAmount));
        }

        return sb.ToString();
    }

    /// <summary>Platforma egasiga kunlik qator (TG17): tenant ma'lumotisiz, faqat sonlar.</summary>
    private async Task RunOpsLineAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IOpsNotifier ops = scope.ServiceProvider.GetRequiredService<IOpsNotifier>();
        if (!ops.IsEnabled) return;

        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        DateTime since = DateTime.UtcNow.AddHours(-24);
        int active = await db.Tenants.CountAsync(t => t.IsActive, ct);
        int sent = await db.TelegramOutboxes.CountAsync(o => o.Status == TelegramOutboxStatus.Sent && o.CreatedAt >= since, ct);
        int failed = await db.TelegramOutboxes.CountAsync(o => o.Status == TelegramOutboxStatus.Failed && o.CreatedAt >= since, ct);
        int pending = await db.TelegramOutboxes.CountAsync(o => o.Status == TelegramOutboxStatus.Pending, ct);

        // Transferlar RLS ostida — tenantlar bo'yicha yig'iladi.
        int transfers = 0;
        await TenantScopes.ForEachTenantAsync(_services, async (sp, _, token) =>
        {
            transfers += await sp.GetRequiredService<WmsDbContext>().Transfers.CountAsync(t => t.CreatedAt >= since, token);
        }, _logger, ct);

        string date = TelegramQuietHours.ToTashkent(DateTime.UtcNow).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        await ops.SendAsync(
            $"📊 <b>WMS {date}</b>\nFaol tenantlar: {active}\nKechagi transferlar: {transfers}\nTelegram navbati (24 s): {sent} yuborildi, {failed} xato, {pending} kutmoqda",
            $"ops:daily:{TelegramQuietHours.ToTashkent(DateTime.UtcNow):yyyyMMdd}", ct);
    }
}

/// <summary>Kunlik xulosa matn kalitlari (Translations).</summary>
public static class DigestKeys
{
    public const string Title = "Daily summary — {0}";
    public const string Pending = "Transfers awaiting confirmation: {0}";
    public const string Production = "Active production orders: {0}";
    public const string LowStock = "Low-stock products: {0}";
    public const string Expiring = "Batches expiring within {0} days: {1}";
    public const string Debt = "Total receivables: {0}";
    public const string TopDebtors = "Largest debtors:";
}
