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
using WMS.Application.DTOs.Kpi;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Xodim buyruqlari (TG14: <c>/keldim</c>, <c>/ketdim</c>, <c>/smena</c>, <c>/kpi</c>) va hisobot fayllari
/// (TG15: <c>/hisobot</c> → Excel). Tenant tanlovi <see cref="TelegramChatContext"/> dan.
/// </summary>
/// <remarks>
/// Davomat feature'ga bog'liq (<c>kpi.attendance</c>), hisobot — <c>export.excel</c>; web'dagi
/// <c>[RequireFeature]</c> bu yerda yo'q, shuning uchun tekshiruv oshkora (<see cref="ITenantStateService"/>).
/// Smena: joriy Toshkent vaqti qaysi smenaga tushsa — o'sha; aniq bo'lmasa tugma bilan so'raladi (<c>sh:</c>).
/// </remarks>
public sealed class TelegramWorkCommands
{
    public const string ShiftPrefix = "sh:";
    public const string ReportPrefix = "rp:";
    private const int MaxRows = 10;
    private const long MaxDocumentBytes = 50L * 1024 * 1024;

    public static readonly IReadOnlySet<string> Commands =
        new HashSet<string>(StringComparer.Ordinal) { "keldim", "ketdim", "smena", "kpi", "hisobot" };

    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramChatContext _chats;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramWorkCommands> _logger;

    public TelegramWorkCommands(IServiceProvider services, ITelegramService telegram, TelegramChatContext chats,
        IOptions<TelegramOptions> options, ILogger<TelegramWorkCommands> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _services = services;
        _telegram = telegram;
        _chats = chats;
        _options = options.Value;
        _logger = logger;
    }

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

    /// <summary>Tanlov tugmalari: <c>sel:</c> (tenant), <c>sh:&lt;shiftN&gt;</c> (smena), <c>rp:&lt;kod&gt;</c> (hisobot).</summary>
    public async Task HandleCallbackAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);
        string data = update.CallbackData ?? string.Empty;

        if (data.StartsWith(TelegramChatContext.SelectPrefix, StringComparison.Ordinal))
        {
            (TelegramConnection? chosen, string command, string? payload) = await _chats.SelectAsync(update.ChatId, data, ct);
            if (chosen is null)
            {
                await AnswerAsync(update, Translations.Format(TelegramCallbacks.ExpiredKey, lang), ct);
                return;
            }

            await AnswerAsync(update, chosen.TenantName, ct);
            await ExecuteAsync(update.ChatId, chosen, command, payload, lang, ct);
            return;
        }

        // sh:/rp: — tenant allaqachon aniq (bitta yoki eslab qolingan); bo'lmasa qayta so'raladi.
        List<TelegramConnection> connections = await _chats.ConnectionsAsync(update.ChatId, ct);
        string cmd = data.StartsWith(ShiftPrefix, StringComparison.Ordinal) ? "keldim" : "hisobot";
        TelegramConnection? c = await _chats.ResolveAsync(update.ChatId, connections, cmd, data[3..], lang, ct);
        if (c is null)
        {
            await AnswerAsync(update, null, ct);
            return;
        }

        await AnswerAsync(update, null, ct);
        if (update.MessageId is { } messageId)
            await _telegram.RemoveReplyMarkupAsync(update.ChatId, messageId, ct);
        await ExecuteAsync(update.ChatId, c, cmd, data[3..], lang, ct);
    }

    private async Task ExecuteAsync(long chatId, TelegramConnection c, string command, string? payload, string lang, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IServiceProvider sp = scope.ServiceProvider;
        sp.GetRequiredService<ICurrentTenant>().Set(c.TenantId, c.TenantCode);
        WmsDbContext db = sp.GetRequiredService<WmsDbContext>();

        WMS.Application.Common.TenantState? state = await sp.GetRequiredService<ITenantStateService>().GetAsync(c.TenantId, ct);
        WmsAccess? access = await sp.GetRequiredService<IWmsAccessResolver>().ResolveAsync(c.IdentitySub, c.TenantId, ct);
        sp.GetRequiredService<WmsAccessContext>().Set(access);

        (string permission, string feature) = command switch
        {
            "hisobot" => (WmsPermissions.DashboardView, FeatureCodes.ExportExcel),
            "kpi" or "smena" => (WmsPermissions.KpiView, FeatureCodes.KpiShifts),
            _ => (WmsPermissions.KpiView, FeatureCodes.KpiAttendance),
        };

        if (state is not null && !state.EnabledFeatures.Contains(feature))
        {
            await SendAsync(chatId, Translations.Format(WorkKeys.NotInPlan, lang), ct);
            return;
        }

        if (access is null || !access.Permissions.Contains(permission))
        {
            await SendAsync(chatId, Translations.Format(TelegramCallbacks.NoPermissionKey, lang), ct);
            return;
        }

        string header = "🏭 <b>" + WebUtility.HtmlEncode(c.TenantName) + "</b>\n";
        try
        {
            string text = command switch
            {
                "keldim" => await CheckInAsync(sp, db, c, payload, chatId, lang, ct),
                "ketdim" => await CheckOutAsync(sp, db, c, lang, ct),
                "smena" => await ShiftsTodayAsync(db, lang, ct),
                "kpi" => await KpiAsync(sp, lang),
                _ => await ReportAsync(sp, db, c, access, payload, chatId, lang, ct),
            };
            if (!string.IsNullOrEmpty(text)) await SendAsync(chatId, header + text, ct);
        }
        catch (AppException ex)
        {
            await SendAsync(chatId, Translations.Format(ex.MessageTemplate, lang, ex.MessageArgs), ct);
        }
    }

    // ── /keldim [sh:<id>] ──
    private async Task<string> CheckInAsync(IServiceProvider sp, WmsDbContext db, TelegramConnection c, string? payload, long chatId, string lang, CancellationToken ct)
    {
        var open = await db.AttendanceLogs.AsNoTracking()
            .Where(a => a.UserId == c.ProfileId && a.CheckOut == null)
            .Select(a => new { a.CheckIn })
            .FirstOrDefaultAsync(ct);
        if (open is not null)
            return Translations.Format(WorkKeys.AlreadyIn, lang, TelegramQuietHours.ToTashkent(open.CheckIn).ToString("HH:mm", CultureInfo.InvariantCulture));

        var shifts = await db.Shifts.AsNoTracking().Select(s => new { s.Id, s.Name, s.StartTime, s.EndTime }).ToListAsync(ct);
        if (shifts.Count == 0) return Translations.Format(WorkKeys.NoShifts, lang);

        Guid? shiftId = null;
        if (payload is { Length: 32 } && Guid.TryParseExact(payload, "N", out Guid chosen) && shifts.Any(s => s.Id == chosen))
        {
            shiftId = chosen;
        }
        else
        {
            // Hozirgi vaqt qaysi smenaga tushadi (tungi smena: boshlanish > tugash).
            TimeSpan now = TelegramQuietHours.ToTashkent(DateTime.UtcNow).TimeOfDay;
            var matching = shifts.Where(s => s.StartTime <= s.EndTime
                    ? now >= s.StartTime && now < s.EndTime
                    : now >= s.StartTime || now < s.EndTime).ToList();
            if (matching.Count == 1) shiftId = matching[0].Id;
            else if (shifts.Count == 1) shiftId = shifts[0].Id;
        }

        if (shiftId is null)
        {
            var rows = shifts.Select(s => new[]
            {
                new { text = $"{s.Name} ({s.StartTime:hh\\:mm}–{s.EndTime:hh\\:mm})", callback_data = $"{ShiftPrefix}{s.Id:N}" },
            }).ToArray();
            await _telegram.SendMessageAsync(chatId, Translations.Format(WorkKeys.WhichShift, lang),
                JsonSerializer.Serialize(new { inline_keyboard = rows }), ct);
            return string.Empty;
        }

        AttendanceLogDto log = await sp.GetRequiredService<IKpiService>().CheckInAsync(new CheckInDto
        {
            UserId = c.ProfileId,
            ShiftId = shiftId.Value,
            Method = AttendanceMethod.Telegram,
            DeviceId = chatId.ToString(CultureInfo.InvariantCulture),
        });

        string shiftName = shifts.First(s => s.Id == shiftId.Value).Name;
        return Translations.Format(WorkKeys.CheckedIn, lang, shiftName, TelegramQuietHours.ToTashkent(log.CheckIn).ToString("HH:mm", CultureInfo.InvariantCulture));
    }

    // ── /ketdim ──
    private static async Task<string> CheckOutAsync(IServiceProvider sp, WmsDbContext db, TelegramConnection c, string lang, CancellationToken ct)
    {
        Guid? openId = await db.AttendanceLogs.AsNoTracking()
            .Where(a => a.UserId == c.ProfileId && a.CheckOut == null)
            .OrderByDescending(a => a.CheckIn)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);
        if (openId is null) return Translations.Format(WorkKeys.NotIn, lang);

        AttendanceLogDto log = await sp.GetRequiredService<IKpiService>().CheckOutAsync(openId.Value);
        TimeSpan worked = (log.CheckOut ?? DateTime.UtcNow) - log.CheckIn;
        return Translations.Format(WorkKeys.CheckedOut, lang,
            TelegramQuietHours.ToTashkent(log.CheckOut ?? DateTime.UtcNow).ToString("HH:mm", CultureInfo.InvariantCulture),
            $"{(int)worked.TotalHours}:{worked.Minutes:00}");
    }

    // ── /smena — bugungi reja (mahsulot × smena) ──
    private static async Task<string> ShiftsTodayAsync(WmsDbContext db, string lang, CancellationToken ct)
    {
        DateTime today = TelegramQuietHours.ToTashkent(DateTime.UtcNow).Date;
        DateTime from = today - TelegramQuietHours.TashkentOffset;
        DateTime to = from.AddDays(1);
        var plans = await db.ShiftPlans.AsNoTracking()
            .Where(p => p.Date >= from && p.Date < to)
            .OrderBy(p => p.Shift.StartTime).ThenBy(p => p.Product.Name)
            .Select(p => new { ShiftName = p.Shift.Name, p.Shift.StartTime, p.Shift.EndTime, ProductName = p.Product.Name, p.PlannedQuantity, Unit = p.Product.Unit.ShortName })
            .Take(MaxRows + 1)
            .ToListAsync(ct);

        if (plans.Count == 0) return Translations.Format(WorkKeys.NoPlans, lang);

        var sb = new StringBuilder();
        sb.Append("🗓 <b>").Append(WebUtility.HtmlEncode(Translations.Format(WorkKeys.PlansTitle, lang, today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)))).Append("</b>\n<pre>");
        foreach (var p in plans.Take(MaxRows))
            sb.Append(WebUtility.HtmlEncode(p.ShiftName)).Append(' ').Append(p.StartTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture))
                .Append('–').Append(p.EndTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture)).Append("  ")
                .Append(WebUtility.HtmlEncode(p.ProductName)).Append(" — ").Append(NotificationMessages.Quantity(p.PlannedQuantity)).Append(' ').Append(WebUtility.HtmlEncode(p.Unit ?? "")).Append('\n');
        sb.Append("</pre>");
        if (plans.Count > MaxRows) sb.Append(Translations.Format(QueryKeys.More, lang, plans.Count - MaxRows));
        return sb.ToString();
    }

    // ── /kpi — shu hafta samaradorlik (smena × mahsulot) ──
    private static async Task<string> KpiAsync(IServiceProvider sp, string lang)
    {
        DateTime local = TelegramQuietHours.ToTashkent(DateTime.UtcNow).Date;
        DateTime weekStart = local.AddDays(-(int)(local.DayOfWeek == DayOfWeek.Sunday ? 6 : local.DayOfWeek - DayOfWeek.Monday)) - TelegramQuietHours.TashkentOffset;
        List<EfficiencyDto> rows = await sp.GetRequiredService<IKpiService>().GetEfficiencyAsync(weekStart, DateTime.UtcNow);
        if (rows.Count == 0) return Translations.Format(WorkKeys.NoKpi, lang);

        var sb = new StringBuilder();
        sb.Append("📈 <b>").Append(WebUtility.HtmlEncode(Translations.Format(WorkKeys.KpiTitle, lang))).Append("</b>\n<pre>");
        foreach (EfficiencyDto r in rows.OrderByDescending(r => r.Date).Take(MaxRows))
            sb.Append(r.Date.ToString("dd.MM", CultureInfo.InvariantCulture)).Append("  ").Append(WebUtility.HtmlEncode(r.ShiftName)).Append("  ")
                .Append(WebUtility.HtmlEncode(r.ProductName)).Append(": ").Append(NotificationMessages.Quantity(r.Actual)).Append('/')
                .Append(NotificationMessages.Quantity(r.Planned)).Append(" (").Append(r.EfficiencyPercent.ToString("0", CultureInfo.InvariantCulture)).Append("%)\n");
        sb.Append("</pre>");
        return sb.ToString();
    }

    // ── /hisobot [rp:<kod>] ──
    private async Task<string> ReportAsync(IServiceProvider sp, WmsDbContext db, TelegramConnection c, WmsAccess access, string? payload, long chatId, string lang, CancellationToken ct)
    {
        (string code, string permission, string key)[] reports =
        [
            ("stock", WmsPermissions.WarehouseView, WorkKeys.ReportStock),
            ("transfers", WmsPermissions.TransfersView, WorkKeys.ReportTransfers),
            ("finance", WmsPermissions.FinanceView, WorkKeys.ReportFinance),
            ("counterparties", WmsPermissions.PartnersView, WorkKeys.ReportCounterparties),
        ];
        var allowed = reports.Where(r => access.Permissions.Contains(r.permission)).ToList();
        if (allowed.Count == 0) return Translations.Format(TelegramCallbacks.NoPermissionKey, lang);

        var chosen = allowed.FirstOrDefault(r => r.code == payload);
        if (chosen.code is null)
        {
            var rows = allowed.Select(r => new[] { new { text = Translations.Format(r.key, lang), callback_data = ReportPrefix + r.code } }).ToArray();
            await _telegram.SendMessageAsync(chatId, Translations.Format(WorkKeys.WhichReport, lang),
                JsonSerializer.Serialize(new { inline_keyboard = rows }), ct);
            return string.Empty;
        }

        IExportService export = sp.GetRequiredService<IExportService>();
        DateTime monthStart = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        byte[] bytes = chosen.code switch
        {
            "stock" => await export.ExportStockAsync(),
            "transfers" => await export.ExportTransfersAsync(monthStart, null),
            "finance" => await export.ExportTransactionsAsync(monthStart, null),
            _ => await export.ExportCounterpartiesAsync(),
        };

        if (bytes.LongLength > MaxDocumentBytes) return Translations.Format(WorkKeys.TooLarge, lang);

        string date = TelegramQuietHours.ToTashkent(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string fileName = $"{c.TenantCode}-{chosen.code}-{date}.xlsx";
        TelegramSendResult result = await _telegram.SendDocumentAsync(chatId, fileName, bytes,
            "🏭 <b>" + WebUtility.HtmlEncode(c.TenantName) + "</b> — " + WebUtility.HtmlEncode(Translations.Format(chosen.key, lang)), ct);
        if (!result.Ok)
            _logger.LogWarning("Telegram: hisobot fayli yuborilmadi (HTTP {StatusCode}: {Description})", result.StatusCode, result.Description);

        _ = db; // hisobot servis orqali (web bilan BIR XIL fayl, ReportBranding)
        return string.Empty;
    }

    private async Task SendAsync(long chatId, string text, CancellationToken ct)
    {
        if (text.Length > TelegramOutbox.MaxTextLength) text = text[..(TelegramOutbox.MaxTextLength - 1)] + "…";
        TelegramSendResult result = await _telegram.SendMessageAsync(chatId, text, null, ct);
        if (!result.Ok)
            _logger.LogInformation("Telegram: javob yuborilmadi (HTTP {StatusCode}: {Description})", result.StatusCode, result.Description);
    }

    private async Task AnswerAsync(TelegramUpdate update, string? text, CancellationToken ct)
    {
        if (update.CallbackId is not null)
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, text, false, ct);
    }
}

/// <summary>TG14/TG15 matn kalitlari (Translations).</summary>
public static class WorkKeys
{
    public const string NotInPlan = "This feature is not included in your plan.";
    public const string AlreadyIn = "You already checked in at {0}.";
    public const string NotIn = "You have not checked in yet — send /keldim first.";
    public const string NoShifts = "No shifts are set up yet.";
    public const string WhichShift = "Which shift?";
    public const string CheckedIn = "✅ Checked in: {0}, {1}.";
    public const string CheckedOut = "👋 Checked out at {0}. Worked: {1}.";
    public const string NoPlans = "No shift plans for today.";
    public const string PlansTitle = "Shift plans — {0}";
    public const string NoKpi = "No efficiency data for this week yet.";
    public const string KpiTitle = "Efficiency this week";
    public const string WhichReport = "Which report?";
    public const string ReportStock = "📦 Stock (Excel)";
    public const string ReportTransfers = "🔄 Transfers — this month (Excel)";
    public const string ReportFinance = "💰 Finance — this month (Excel)";
    public const string ReportCounterparties = "🤝 Counterparties (Excel)";
    public const string TooLarge = "The report is too large for Telegram — download it from the web app.";
}
