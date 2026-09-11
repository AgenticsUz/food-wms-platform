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
using WMS.Application.DTOs.Analytics;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// So'rov buyruqlari (TG10): <c>/qoldiq &lt;nom&gt;</c>, <c>/muddat</c>, <c>/qarz</c>, <c>/bugun</c>, <c>/kutilmoqda</c>.
/// </summary>
/// <remarks>
/// <para>
/// Chat bir necha tenantga ulangan bo'lsa avval tenant so'raladi (inline tugmalar, <c>sel:&lt;tenant&gt;:&lt;buyruq&gt;</c>)
/// va tanlov bir soat eslab qolinadi (<c>telegram_chat_state</c>, platforma jadvali). Bitta tenant — so'ralmaydi.
/// </para>
/// <para>
/// Ruxsat — o'sha RBAC bazasidan, buyruq bajarilgan paytda. Telegram buyruq menyusi ruxsatga qarab
/// yashirilmaydi (u chat darajasida) — ruxsatsiz buyruq «ruxsat yo'q» oladi.
/// Javoblar <c>&lt;pre&gt;</c> jadval, 4096 chegara; ortig'i «… va yana N».
/// </para>
/// </remarks>
public sealed class TelegramQueryCommands
{
    public const string SelectPrefix = TelegramChatContext.SelectPrefix;
    private const int MaxRows = 15;
    private const int ExpiringDays = 7;
    private const int PendingButtons = 10;

    public static readonly IReadOnlySet<string> Commands =
        new HashSet<string>(StringComparer.Ordinal) { "qoldiq", "muddat", "qarz", "bugun", "kutilmoqda" };

    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramChatContext _chats;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramQueryCommands> _logger;

    public TelegramQueryCommands(IServiceProvider services, ITelegramService telegram, TelegramChatContext chats,
        IOptions<TelegramOptions> options, ILogger<TelegramQueryCommands> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _services = services;
        _telegram = telegram;
        _chats = chats;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Buyruq: tenantni aniqlaydi (kerak bo'lsa so'raydi) va bajaradi.</summary>
    public async Task HandleCommandAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);
        string command = update.Command ?? string.Empty;

        List<TelegramConnection> connections = await _chats.ConnectionsAsync(update.ChatId, ct);
        if (connections.Count == 0)
        {
            await SendAsync(update.ChatId, TelegramBotReplies.Status([], lang), ct);
            return;
        }

        TelegramConnection? chosen = await _chats.ResolveAsync(update.ChatId, connections, command, update.Payload, lang, ct);
        if (chosen is not null)
            await ExecuteAsync(update.ChatId, chosen, command, update.Payload, lang, ct);
    }

    /// <summary>Tenant tanlash tugmasi: <c>sel:&lt;tenantN&gt;:&lt;buyruq&gt;</c>.</summary>
    public async Task HandleSelectionAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);
        (TelegramConnection? chosen, string command, string? payload) = await _chats.SelectAsync(update.ChatId, update.CallbackData, ct);
        if (chosen is null)
        {
            await AnswerAsync(update, Translations.Format(TelegramCallbacks.ExpiredKey, lang), ct);
            return;
        }

        await AnswerAsync(update, chosen.TenantName, ct);
        if (update.MessageId is { } messageId)
            await _telegram.EditMessageTextAsync(update.ChatId, messageId, "🏭 " + WebUtility.HtmlEncode(chosen.TenantName), ct);

        await ExecuteAsync(update.ChatId, chosen, command, payload, lang, ct);
    }

    private async Task ExecuteAsync(long chatId, TelegramConnection c, string command, string? payload, string lang, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IServiceProvider sp = scope.ServiceProvider;
        sp.GetRequiredService<ICurrentTenant>().Set(c.TenantId, c.TenantCode);
        WmsDbContext db = sp.GetRequiredService<WmsDbContext>();

        WmsAccess? access = await sp.GetRequiredService<IWmsAccessResolver>().ResolveAsync(c.IdentitySub, c.TenantId, ct);
        string needed = command switch
        {
            "qoldiq" or "muddat" => WmsPermissions.WarehouseView,
            "qarz" => WmsPermissions.FinanceView,
            "kutilmoqda" => WmsPermissions.TransfersView,
            _ => WmsPermissions.DashboardView,
        };
        if (access is null || !access.Permissions.Contains(needed))
        {
            await SendAsync(chatId, Translations.Format(TelegramCallbacks.NoPermissionKey, lang), ct);
            return;
        }

        string header = "🏭 <b>" + WebUtility.HtmlEncode(c.TenantName) + "</b>\n";
        string text = command switch
        {
            "qoldiq" => await StockAsync(db, payload, lang, ct),
            "muddat" => await ExpiringAsync(db, lang, ct),
            "qarz" => await DebtsAsync(sp, lang),
            "bugun" => await TodayAsync(sp, db, lang, ct),
            "kutilmoqda" => await PendingAsync(db, c, lang, ct),
            _ => Translations.Format(TelegramBotReplies.CommandsKey, lang),
        };

        if (!string.IsNullOrEmpty(text))
            await SendAsync(chatId, header + text, ct);
    }

    // ── /qoldiq <nom> ──
    private static async Task<string> StockAsync(WmsDbContext db, string? query, string lang, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Translations.Format(QueryKeys.StockUsage, lang);

        string pattern = $"%{query.Trim()}%";
        var rows = await db.WarehouseStocks.AsNoTracking()
            .Where(s => EF.Functions.ILike(s.Product.Name, pattern))
            .GroupBy(s => new { s.ProductId, ProductName = s.Product.Name, Unit = s.Product.Unit.ShortName, s.Product.MinStock, WarehouseName = s.Warehouse.Name })
            .Select(g => new { g.Key.ProductId, g.Key.ProductName, g.Key.Unit, g.Key.MinStock, g.Key.WarehouseName, Quantity = g.Sum(s => s.Quantity) })
            .OrderBy(r => r.ProductName).ThenBy(r => r.WarehouseName)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return Translations.Format(QueryKeys.NotFound, lang, WebUtility.HtmlEncode(query.Trim()));

        var sb = new StringBuilder();
        sb.Append("📦 <b>").Append(WebUtility.HtmlEncode(Translations.Format(QueryKeys.StockTitle, lang))).Append("</b>\n<pre>");
        int shown = 0;
        foreach (var product in rows.GroupBy(r => r.ProductId).Take(5))
        {
            var first = product.First();
            decimal total = product.Sum(r => r.Quantity);
            string flag = first.MinStock > 0 && total <= first.MinStock ? " ⚠️" : string.Empty;
            sb.Append(WebUtility.HtmlEncode(first.ProductName)).Append(" — ")
                .Append(NotificationMessages.Quantity(total)).Append(' ').Append(WebUtility.HtmlEncode(first.Unit ?? "")).Append(flag).Append('\n');
            foreach (var r in product)
            {
                if (++shown > MaxRows) break;
                sb.Append("  ").Append(WebUtility.HtmlEncode(r.WarehouseName)).Append(": ")
                    .Append(NotificationMessages.Quantity(r.Quantity)).Append('\n');
            }
        }
        sb.Append("</pre>");
        return sb.ToString();
    }

    // ── /muddat ──
    private static async Task<string> ExpiringAsync(WmsDbContext db, string lang, CancellationToken ct)
    {
        DateTime today = DateTime.UtcNow.Date;
        DateTime cutoff = today.AddDays(ExpiringDays + 1);
        var rows = await db.Batches.AsNoTracking()
            .Where(b => b.RemainingQuantity > 0 && b.ExpiryDate != null && b.ExpiryDate < cutoff)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new { b.LotNumber, ProductName = b.Product.Name, b.ExpiryDate, b.RemainingQuantity })
            .Take(MaxRows + 1)
            .ToListAsync(ct);

        if (rows.Count == 0) return Translations.Format(QueryKeys.NoExpiring, lang, ExpiringDays);

        var sb = new StringBuilder();
        sb.Append("⏳ <b>").Append(WebUtility.HtmlEncode(Translations.Format(QueryKeys.ExpiringTitle, lang, ExpiringDays))).Append("</b>\n<pre>");
        foreach (var r in rows.Take(MaxRows))
        {
            string when = r.ExpiryDate!.Value.Date < today ? "⛔" : r.ExpiryDate.Value.ToString("dd.MM", CultureInfo.InvariantCulture);
            sb.Append(when).Append("  ").Append(WebUtility.HtmlEncode(r.ProductName)).Append(" #").Append(WebUtility.HtmlEncode(r.LotNumber))
                .Append(" — ").Append(NotificationMessages.Quantity(r.RemainingQuantity)).Append('\n');
        }
        sb.Append("</pre>");
        if (rows.Count > MaxRows) sb.Append(Translations.Format(QueryKeys.More, lang, rows.Count - MaxRows));
        return sb.ToString();
    }

    // ── /qarz ──
    private static async Task<string> DebtsAsync(IServiceProvider sp, string lang)
    {
        List<TopDebtorDto> debtors = await sp.GetRequiredService<IAnalyticsService>().GetTopDebtors(MaxRows);
        if (debtors.Count == 0) return Translations.Format(QueryKeys.NoDebts, lang);

        var sb = new StringBuilder();
        sb.Append("💰 <b>").Append(WebUtility.HtmlEncode(Translations.Format(QueryKeys.DebtsTitle, lang))).Append("</b>\n<pre>");
        foreach (TopDebtorDto d in debtors)
            sb.Append(WebUtility.HtmlEncode(d.CounterpartyName)).Append(" — ").Append(NotificationMessages.Amount(d.DebtAmount)).Append('\n');
        sb.Append("</pre>");
        return sb.ToString();
    }

    // ── /bugun ──
    private static async Task<string> TodayAsync(IServiceProvider sp, WmsDbContext db, string lang, CancellationToken ct)
    {
        DashboardSummaryDto s = await sp.GetRequiredService<IAnalyticsService>().GetDashboardSummary();
        DateTime dayStart = TelegramQuietHours.ToTashkent(DateTime.UtcNow).Date - TelegramQuietHours.TashkentOffset;
        int transfersToday = await db.Transfers.CountAsync(t => t.CreatedAt >= dayStart, ct);
        DateTime cutoff = DateTime.UtcNow.Date.AddDays(ExpiringDays + 1);
        int expiring = await db.Batches.CountAsync(b => b.RemainingQuantity > 0 && b.ExpiryDate != null && b.ExpiryDate < cutoff, ct);

        var sb = new StringBuilder();
        sb.Append("📋 <b>").Append(WebUtility.HtmlEncode(Translations.Format(QueryKeys.TodayTitle, lang))).Append("</b>\n");
        sb.Append("🔄 ").Append(Translations.Format(QueryKeys.TransfersToday, lang, transfersToday)).Append('\n');
        sb.Append("🕓 ").Append(Translations.Format(DigestKeys.Pending, lang, s.PendingTransfers)).Append('\n');
        sb.Append("🏭 ").Append(Translations.Format(DigestKeys.Production, lang, s.ActiveProductionOrders)).Append('\n');
        sb.Append("⚠️ ").Append(Translations.Format(DigestKeys.LowStock, lang, s.LowStockProductCount)).Append('\n');
        sb.Append("⏳ ").Append(Translations.Format(DigestKeys.Expiring, lang, ExpiringDays, expiring)).Append('\n');
        sb.Append("💰 ").Append(Translations.Format(DigestKeys.Debt, lang, NotificationMessages.Amount(s.TotalDebt)));
        return sb.ToString();
    }

    // ── /kutilmoqda — har transfer alohida, tugmalar bilan (navbat orqali: callback tenant/aktorni navbat qatoridan oladi) ──
    private async Task<string> PendingAsync(WmsDbContext db, TelegramConnection c, string lang, CancellationToken ct)
    {
        var pending = await db.Transfers.AsNoTracking()
            .Where(t => t.Status == TransferStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id, t.Type, t.CreatedAt,
                Party = t.Counterparty != null ? t.Counterparty.Name : (t.FromWarehouse != null ? t.FromWarehouse.Name : (t.ToWarehouse != null ? t.ToWarehouse.Name : "—")),
                Amount = t.Items.Sum(i => i.Quantity * i.UnitPrice),
            })
            .Take(PendingButtons + 1)
            .ToListAsync(ct);

        if (pending.Count == 0) return Translations.Format(QueryKeys.NoPending, lang);

        string? link = string.IsNullOrWhiteSpace(_options.WebUrl) ? null : _options.WebUrl.TrimEnd('/');
        string tenantName = await db.Tenants.AsNoTracking().Where(t => t.Id == c.TenantId).Select(t => t.Name).FirstAsync(ct);

        foreach (var t in pending.Take(PendingButtons))
        {
            string typeKey = t.Type switch
            {
                TransferType.Incoming => NotificationMessages.IncomingPending,
                TransferType.Internal => NotificationMessages.InternalPending,
                TransferType.Return => NotificationMessages.ReturnPending,
                _ => NotificationMessages.SalePending,
            };
            string[] args = t.Type == TransferType.Internal
                ? [t.Party, "—", NotificationMessages.Date(t.CreatedAt), "—"]
                : [t.Party, NotificationMessages.Date(t.CreatedAt), NotificationMessages.Amount(t.Amount), "—"];

            Notification pseudo = new()
            {
                Id = Guid.Empty, // navbat qatori bildirishnomaga bog'lanmaydi
                Title = NotificationMessages.TransferPendingTitle,
                Message = string.Empty,
                MessageTemplate = typeKey,
                MessageArgs = JsonSerializer.Serialize(args),
                Type = NotificationType.TransferPending,
                EntityType = "Transfer",
                EntityId = t.Id,
            };
            string url = link is null ? string.Empty : $"{link}/transfers/{t.Id:D}";
            string text = TelegramOutboxComposer.Text(tenantName, pseudo, lang, null);
            string? markup = TelegramOutboxComposer.ReplyMarkup(pseudo, lang, string.IsNullOrEmpty(url) ? null : url);

            db.TelegramOutboxes.Add(new TelegramOutbox
            {
                TenantId = c.TenantId,
                TelegramLinkId = c.LinkId,
                ChatId = c.ChatId,
                Text = text,
                ReplyMarkup = markup,
                NotificationId = null,
                DedupKey = null, // qayta so'ralsa qayta ko'rsatiladi
            });
        }

        await db.SaveChangesAsync(ct);
        return pending.Count > PendingButtons ? Translations.Format(QueryKeys.More, lang, pending.Count - PendingButtons) : string.Empty;
    }

    private async Task SendAsync(long chatId, string text, CancellationToken ct)
    {
        if (text.Length > TelegramOutbox.MaxTextLength) text = text[..(TelegramOutbox.MaxTextLength - 1)] + "…";
        TelegramSendResult result = await _telegram.SendMessageAsync(chatId, text, null, ct);
        if (!result.Ok)
            _logger.LogInformation("Telegram: so'rov javobi yuborilmadi (HTTP {StatusCode}: {Description})", result.StatusCode, result.Description);
    }

    private async Task AnswerAsync(TelegramUpdate update, string text, CancellationToken ct)
    {
        if (update.CallbackId is not null)
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, text, false, ct);
    }
}

/// <summary>So'rov buyruqlari matn kalitlari (Translations).</summary>
public static class QueryKeys
{
    public const string WhichTenant = "Which organization?";
    public const string StockUsage = "Type the product name: /qoldiq plombir";
    public const string StockTitle = "Stock";
    public const string NotFound = "Nothing found for \"{0}\".";
    public const string ExpiringTitle = "Batches expiring within {0} days";
    public const string NoExpiring = "No batches expire within {0} days.";
    public const string DebtsTitle = "Receivables";
    public const string NoDebts = "No outstanding receivables.";
    public const string TodayTitle = "Today";
    public const string TransfersToday = "Transfers created today: {0}";
    public const string NoPending = "No transfers are waiting for confirmation.";
    public const string More = "… and {0} more";
}
