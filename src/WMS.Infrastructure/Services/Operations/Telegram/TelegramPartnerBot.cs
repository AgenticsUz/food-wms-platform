using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Common;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Haydovchi (TG12) va kontragent (TG13) botи: marshrut yuborish, tugmalar (<c>dd:</c> yetkazildi,
/// <c>df:</c> yetkazilmadi, <c>dw:</c> yuk xati), <c>/marshrut</c>, <c>/qarzim</c>, mijoz xabarlari.
/// </summary>
/// <remarks>
/// <para>
/// <b>Aktor.</b> Haydovchi va mijoz — profil emas, ruxsati yo'q: ular faqat O'Z yetkazishi / o'z balansi
/// bilan ishlaydi. Tugma bosilganda navbat qatori → tenant → ulanish (<c>driver_id</c>) → to'xtash shu
/// haydovchining yetkazishidami — tekshiriladi. Audit <c>Path = telegram:driver</c>, aktor ismi haydovchi.
/// </para>
/// <para>
/// <b>Mijoz roziligi.</b> Kontragentga xabar faqat <c>Tenant.ClientTelegramEnabled</c> bo'lsa (sukut o'chiq):
/// mijozning mijoziga biz xabar yuboramiz — bu tenant qarori (Wash §4.2 dalili).
/// </para>
/// </remarks>
public sealed class TelegramPartnerBot : ITelegramPartnerNotifier
{
    public const string DeliveredPrefix = "dd:";
    public const string FailedPrefix = "df:";
    public const string WaybillPrefix = "dw:";

    public static readonly IReadOnlySet<string> Commands =
        new HashSet<string>(StringComparer.Ordinal) { "marshrut", "qarzim" };

    private readonly IServiceProvider _services;
    private readonly WmsDbContext _db;
    private readonly ITelegramService _telegram;
    private readonly ITenantStateService _tenantState;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramPartnerBot> _logger;

    public TelegramPartnerBot(IServiceProvider services, WmsDbContext db, ITelegramService telegram,
        ITenantStateService tenantState, IOptions<TelegramOptions> options, ILogger<TelegramPartnerBot> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _services = services;
        _db = db;
        _telegram = telegram;
        _tenantState = tenantState;
        _options = options.Value;
        _logger = logger;
    }

    // ═══════════════ ITelegramPartnerNotifier (joriy tenant kontekstida) ═══════════════

    public async Task SendRouteAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        if (!_telegram.IsEnabled) return;

        var delivery = await _db.Deliveries.AsNoTracking()
            .Where(d => d.Id == deliveryId && d.DriverId != null)
            .Select(d => new
            {
                d.Id, d.ScheduledDate, d.Status,
                Link = _db.TelegramLinks.Where(l => l.DriverId == d.DriverId && l.IsActive).Select(l => new { l.Id, l.ChatId, l.Lang }).FirstOrDefault(),
                Stops = d.Stops.OrderBy(s => s.SequenceOrder).Select(s => new { s.Id, s.SequenceOrder, s.Status, s.Address, CounterpartyName = s.Counterparty.Name }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (delivery?.Link is null || delivery.Stops.Count == 0) return;

        Guid? tenantId = _db.CurrentTenantId;
        string? tenantName = tenantId is { } tid ? (await _tenantState.GetAsync(tid, cancellationToken))?.Name : null;
        string lang = TelegramLanguage.Resolve(delivery.Link.Lang, _options.DefaultLanguage);

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(tenantName)) sb.Append("🏭 <b>").Append(WebUtility.HtmlEncode(tenantName)).Append("</b>\n");
        sb.Append("🚚 <b>").Append(WebUtility.HtmlEncode(Translations.Format(PartnerKeys.RouteTitle, lang, NotificationMessages.Date(delivery.ScheduledDate)))).Append("</b>\n");

        List<object[]> rows = [];
        foreach (var s in delivery.Stops)
        {
            string mark = s.Status switch { DeliveryStopStatus.Delivered => "✅", DeliveryStopStatus.Failed => "❌", _ => "▫️" };
            sb.Append(mark).Append(' ').Append(s.SequenceOrder).Append(". ").Append(WebUtility.HtmlEncode(s.CounterpartyName));
            if (!string.IsNullOrWhiteSpace(s.Address)) sb.Append(" — ").Append(WebUtility.HtmlEncode(s.Address));
            sb.Append('\n');

            if (s.Status == DeliveryStopStatus.Pending)
            {
                string id = s.Id.ToString("N", CultureInfo.InvariantCulture);
                string name = s.CounterpartyName.Length > 20 ? s.CounterpartyName[..20] + "…" : s.CounterpartyName;
                rows.Add([
                    new { text = $"✅ {s.SequenceOrder}. {name}", callback_data = DeliveredPrefix + id },
                    new { text = "❌", callback_data = FailedPrefix + id },
                ]);
            }
        }

        rows.Add([new { text = Translations.Format(PartnerKeys.WaybillButton, lang), callback_data = WaybillPrefix + delivery.Id.ToString("N", CultureInfo.InvariantCulture) }]);

        TelegramOutbox route = new()
        {
            TenantId = tenantId,
            TelegramLinkId = delivery.Link.Id,
            ChatId = delivery.Link.ChatId,
            Text = sb.ToString(),
            ReplyMarkup = JsonSerializer.Serialize(new { inline_keyboard = rows }),
            DedupKey = $"route:{delivery.Id:N}:{delivery.Status}:{DateTime.UtcNow:yyyyMMddHH}",
        };
        _db.TelegramOutboxes.Add(route);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
        {
            // Bir soat ichida o'sha marshrut qayta yuborilmoqchi — dedup kaliti ayni shuni
            // to'sadi va bu XATO emas. Ilgari 23505 chaqiruvchiga (yetkazish so'roviga)
            // chiqib ketardi: haydovchi xabarni olgan, operator esa 500 ko'rardi.
            _db.Entry(route).State = EntityState.Detached;
        }
    }

    public async Task NotifyClientAsync(Guid counterpartyId, string template, string?[] args, string? dedupKey = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!_telegram.IsEnabled || _db.CurrentTenantId is not { } tenantId) return;

        var tenant = await _db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => new { t.Name, t.ClientTelegramEnabled }).FirstOrDefaultAsync(cancellationToken);
        if (tenant is null || !tenant.ClientTelegramEnabled) return;

        var link = await _db.TelegramLinks.AsNoTracking()
            .Where(l => l.CounterpartyId == counterpartyId && l.IsActive)
            .Select(l => new { l.Id, l.ChatId, l.Lang })
            .FirstOrDefaultAsync(cancellationToken);
        if (link is null) return;

        if (dedupKey is not null && await _db.TelegramOutboxes.AnyAsync(o => o.DedupKey == dedupKey, cancellationToken)) return;

        string lang = TelegramLanguage.Resolve(link.Lang, _options.DefaultLanguage);
        string?[] full = [tenant.Name, .. args];
        string text = "🏭 <b>" + WebUtility.HtmlEncode(tenant.Name) + "</b>\n" + WebUtility.HtmlEncode(Translations.Format(template, lang, full));

        TelegramOutbox outbox = new()
        {
            TenantId = tenantId,
            TelegramLinkId = link.Id,
            ChatId = link.ChatId,
            Text = text,
            DedupKey = dedupKey,
            NextAttemptAt = TelegramQuietHours.HoldUntil(DateTime.UtcNow, NotificationType.Info, _options),
        };
        _db.TelegramOutboxes.Add(outbox);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
        {
            // Dedup kaliti bo'yicha poyga: yuqoridagi tekshiruvdan keyin boshqa so'rov yozib
            // ulgurgan. Xabar navbatda — ish BAJARILGAN. ⚠️ Yozuv kuzatuvdan chiqariladi,
            // aks holda shu qamrovdagi keyingi `SaveChanges` uni qayta urinib yiqilardi.
            _db.Entry(outbox).State = EntityState.Detached;
        }
    }

    // ═══════════════ Bot tomoni (tenant kontekstisiz chaqiriladi) ═══════════════

    /// <summary>Chat haydovchi yoki kontragent sifatida qaysi tenantga ulangan.</summary>
    private sealed record PartnerLink(Guid TenantId, string TenantCode, string TenantName, Guid LinkId, Guid? DriverId, Guid? CounterpartyId, string Lang, string Name);

    private async Task<List<PartnerLink>> PartnerLinksAsync(long chatId, CancellationToken ct)
    {
        List<PartnerLink> result = [];
        await TenantScopes.ForEachTenantAsync(_services, async (scope, tenantId, token) =>
        {
            WmsDbContext db = scope.GetRequiredService<WmsDbContext>();
            var found = await db.TelegramLinks.AsNoTracking()
                .Where(l => l.ChatId == chatId && l.IsActive && (l.DriverId != null || l.CounterpartyId != null))
                .Select(l => new { l.Id, l.DriverId, l.CounterpartyId, l.Lang, Name = l.Driver != null ? l.Driver.FullName : l.Counterparty!.Name })
                .ToListAsync(token);
            if (found.Count == 0) return;

            var tenant = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => new { t.Code, t.Name }).FirstAsync(token);
            foreach (var f in found)
                result.Add(new PartnerLink(tenantId, tenant.Code, tenant.Name, f.Id, f.DriverId, f.CounterpartyId, f.Lang, f.Name));
        }, _logger, ct);
        return result;
    }

    /// <summary><c>/marshrut</c> (haydovchi) va <c>/qarzim</c> (kontragent). Ulanish yo'q — <see langword="false"/>.</summary>
    public async Task<bool> HandleCommandAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        List<PartnerLink> links = await PartnerLinksAsync(update.ChatId, ct);
        if (links.Count == 0) return false;

        foreach (PartnerLink link in links)
        {
            await using AsyncServiceScope scope = _services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(link.TenantId, link.TenantCode);
            string lang = TelegramLanguage.Resolve(link.Lang, _options.DefaultLanguage);

            if (update.Command == "marshrut" && link.DriverId is { } driverId)
                await SendTodayRoutesAsync(scope.ServiceProvider, driverId, update.ChatId, lang, ct);
            else if (update.Command == "qarzim" && link.CounterpartyId is { } counterpartyId)
                await SendBalanceAsync(scope.ServiceProvider, link, counterpartyId, update.ChatId, lang, ct);
        }

        return true;
    }

    private async Task SendTodayRoutesAsync(IServiceProvider sp, Guid driverId, long chatId, string lang, CancellationToken ct)
    {
        WmsDbContext db = sp.GetRequiredService<WmsDbContext>();
        DateTime dayStart = TelegramQuietHours.ToTashkent(DateTime.UtcNow).Date - TelegramQuietHours.TashkentOffset;
        List<Guid> ids = await db.Deliveries.AsNoTracking()
            .Where(d => d.DriverId == driverId && d.Status != DeliveryStatus.Cancelled && d.Status != DeliveryStatus.Completed
                && d.ScheduledDate >= dayStart.AddDays(-1) && d.ScheduledDate < dayStart.AddDays(2))
            .OrderBy(d => d.ScheduledDate)
            .Select(d => d.Id)
            .ToListAsync(ct);

        if (ids.Count == 0)
        {
            await _telegram.SendMessageAsync(chatId, Translations.Format(PartnerKeys.NoRoutes, lang), null, ct);
            return;
        }

        ITelegramPartnerNotifier notifier = sp.GetRequiredService<ITelegramPartnerNotifier>();
        foreach (Guid id in ids) await notifier.SendRouteAsync(id, ct);
    }

    private async Task SendBalanceAsync(IServiceProvider sp, PartnerLink link, Guid counterpartyId, long chatId, string lang, CancellationToken ct)
    {
        WmsDbContext db = sp.GetRequiredService<WmsDbContext>();
        decimal debt = await db.Debts.AsNoTracking().Where(d => d.CounterpartyId == counterpartyId).Select(d => d.Amount).FirstOrDefaultAsync(ct);
        // ⚠️ Oxirgi to'lovlar — HUJJAT sanasi bo'yicha (P2.3): mijoz «kecha to'ladim» deydi va
        // haq bo'ladi, `CreatedAt` esa operator qachon kiritganini ko'rsatardi. Tenglikda
        // yozuv lahzasi — bir kundagi ikki to'lov kiritilish tartibida chiqsin.
        var payments = await db.PaymentHistories.AsNoTracking()
            .Where(p => p.CounterpartyId == counterpartyId)
            .OrderByDescending(p => p.DocumentDate).ThenByDescending(p => p.CreatedAt).Take(5)
            .Select(p => new { p.DocumentDate, p.Amount })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.Append("🏭 <b>").Append(WebUtility.HtmlEncode(link.TenantName)).Append("</b>\n");
        sb.Append(debt > 0
            ? Translations.Format(NotificationMessages.ClientBalance, lang, WebUtility.HtmlEncode(link.TenantName), NotificationMessages.Amount(debt))
            : Translations.Format(NotificationMessages.ClientNoDebt, lang, WebUtility.HtmlEncode(link.TenantName)));
        if (payments.Count > 0)
        {
            sb.Append("\n\n").Append(Translations.Format(PartnerKeys.RecentPayments, lang)).Append("<pre>");
            foreach (var p in payments)
                sb.Append('\n').Append(NotificationMessages.Date(p.DocumentDate)).Append("  ").Append(NotificationMessages.Amount(p.Amount));
            sb.Append("</pre>");
        }

        await _telegram.SendMessageAsync(chatId, sb.ToString(), null, ct);
    }

    /// <summary><c>dd:</c>/<c>df:</c>/<c>dw:</c> — haydovchi tugmalari.</summary>
    public async Task HandleCallbackAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (update.CallbackId is null || update.MessageId is null) return;

        string data = update.CallbackData ?? string.Empty;
        string action = data.Length >= 3 ? data[..3] : string.Empty;
        if (!Guid.TryParseExact(data.Length > 3 ? data[3..] : string.Empty, "N", out Guid id))
        {
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, Translations.Format(TelegramCallbacks.ExpiredKey, _options.DefaultLanguage), true, ct);
            return;
        }

        // Navbat qatori → tenant va ulanish.
        Guid tenantId; Guid linkId; string tenantCode;
        await using (AsyncServiceScope lookup = _services.CreateAsyncScope())
        {
            WmsDbContext db = lookup.ServiceProvider.GetRequiredService<WmsDbContext>();
            var row = await (
                from o in db.TelegramOutboxes.AsNoTracking()
                join t in db.Tenants.AsNoTracking() on o.TenantId equals t.Id
                where o.ChatId == update.ChatId && o.MessageId == update.MessageId && o.TelegramLinkId != null && t.IsActive
                select new { o.TenantId, o.TelegramLinkId, t.Code }).FirstOrDefaultAsync(ct);
            if (row is null)
            {
                await _telegram.AnswerCallbackQueryAsync(update.CallbackId, Translations.Format(TelegramCallbacks.ExpiredKey, _options.DefaultLanguage), true, ct);
                return;
            }
            (tenantId, linkId, tenantCode) = (row.TenantId!.Value, row.TelegramLinkId!.Value, row.Code);
        }

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IServiceProvider sp = scope.ServiceProvider;
        sp.GetRequiredService<ICurrentTenant>().Set(tenantId, tenantCode);
        WmsDbContext tdb = sp.GetRequiredService<WmsDbContext>();

        var driver = await tdb.TelegramLinks.AsNoTracking()
            .Where(l => l.Id == linkId && l.IsActive && l.DriverId != null && l.Driver!.IsActive)
            .Select(l => new { DriverId = l.DriverId!.Value, l.Driver!.FullName, l.Lang })
            .FirstOrDefaultAsync(ct);
        string lang = TelegramLanguage.Resolve(driver?.Lang, _options.DefaultLanguage);
        if (driver is null)
        {
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, Translations.Format(TelegramCallbacks.ExpiredKey, lang), true, ct);
            return;
        }

        IDeliveryService deliveries = sp.GetRequiredService<IDeliveryService>();
        try
        {
            if (action == WaybillPrefix)
            {
                bool own = await tdb.Deliveries.AnyAsync(d => d.Id == id && d.DriverId == driver.DriverId, ct);
                if (!own) throw new AppException(TelegramCallbacks.ExpiredKey);
                byte[] pdf = await sp.GetRequiredService<IDeliveryPdfService>().GenerateWaybillPdfAsync(id, ct);
                await _telegram.AnswerCallbackQueryAsync(update.CallbackId, null, false, ct);
                await _telegram.SendDocumentAsync(update.ChatId, $"waybill-{NotificationMessages.Date(DateTime.UtcNow)}.pdf", pdf, null, ct);
                return;
            }

            // To'xtash shu haydovchining yetkazishidami — begona to'xtashni yopib bo'lmasin.
            var stop = await tdb.DeliveryStops.AsNoTracking()
                .Where(s => s.Id == id && s.Delivery.DriverId == driver.DriverId)
                .Select(s => new { s.DeliveryId, s.Status, CounterpartyName = s.Counterparty.Name, s.CounterpartyId })
                .FirstOrDefaultAsync(ct);
            if (stop is null) throw new AppException(TelegramCallbacks.ExpiredKey);
            if (stop.Status != DeliveryStopStatus.Pending) throw new AppException(TelegramCallbacks.AlreadyKey);

            if (action == DeliveredPrefix)
            {
                await deliveries.MarkStopDeliveredAsync(stop.DeliveryId, id);
                await sp.GetRequiredService<ITelegramPartnerNotifier>().NotifyClientAsync(stop.CounterpartyId,
                    NotificationMessages.ClientDelivered, [], $"client:delivered:{id:N}", ct);
            }
            else
            {
                await deliveries.MarkStopFailedAsync(stop.DeliveryId, id, null);
                await sp.GetRequiredService<INotificationService>().NotifyAsync(null,
                    NotificationMessages.DeliveryStopFailedTitle, NotificationMessages.DeliveryStopFailed,
                    [driver.FullName, stop.CounterpartyName, NotificationMessages.Date(DateTime.UtcNow)],
                    NotificationType.DeliveryStopFailed, "Delivery", stop.DeliveryId);
            }

            await WriteAuditAsync(tdb, driver.FullName, action == DeliveredPrefix ? "StopDelivered" : "StopFailed", id, update.UpdateId, ct);
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId,
                Translations.Format(action == DeliveredPrefix ? PartnerKeys.StopDelivered : PartnerKeys.StopFailed, lang, stop.CounterpartyName), false, ct);

            // Marshrut xabari yangilanadi — qolgan to'xtashlar tugmalari bilan.
            await _telegram.RemoveReplyMarkupAsync(update.ChatId, update.MessageId.Value, ct);
            await sp.GetRequiredService<ITelegramPartnerNotifier>().SendRouteAsync(stop.DeliveryId, ct);
        }
        catch (AppException ex)
        {
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, Translations.Format(ex.MessageTemplate, lang, ex.MessageArgs), true, ct);
        }
    }

    private static async Task WriteAuditAsync(WmsDbContext db, string driverName, string entityAction, Guid stopId, long updateId, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorSub = null,
            UserName = driverName,
            Action = "PUT",
            EntityType = "Delivery",
            EntityAction = entityAction,
            EntityId = stopId.ToString("D", CultureInfo.InvariantCulture),
            Path = "telegram:driver",
            StatusCode = 200,
            IsPlatformAction = false,
            CorrelationId = "tg:" + updateId.ToString(CultureInfo.InvariantCulture),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Haydovchi/mijoz matn kalitlari (Translations).</summary>
public static class PartnerKeys
{
    public const string RouteTitle = "Route — {0}";
    public const string WaybillButton = "📄 Waybill (PDF)";
    public const string NoRoutes = "No deliveries planned for you today.";
    public const string StopDelivered = "✅ Delivered: {0}";
    public const string StopFailed = "❌ Not delivered: {0}";
    public const string RecentPayments = "Recent payments:";
    public const string DriverLinked = "✅ Connected as a driver of <b>{0}</b>. Your routes will arrive here; /marshrut — today's route.";
    public const string ClientLinked = "✅ Connected to <b>{0}</b>. You will receive order and delivery updates here; /qarzim — your balance.";
}
