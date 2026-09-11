using System.Net;
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
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Guruh chati (TG16): <c>/ulash</c> — admin guruhni tenantga bog'laydi, <c>/sozlash</c> — qaysi turlar
/// kelsin, <c>/uzish</c>. Bot guruhdan chiqarilsa guruh o'chadi.
/// </summary>
/// <remarks>
/// Admin — guruhda yozgan odamning SHAXSIY ulanishi bo'lgan va <c>settings.modules</c> ruxsatli tenant(lar)i.
/// Bir nechta bo'lsa tugma bilan so'raladi (<c>gl:&lt;tenant&gt;</c>). Guruhda moliya/qoldiq so'rovlari
/// YO'Q (v1) — javob hammaga ko'rinadi; faqat umumiy bildirishnomalar va tugmalar.
/// </remarks>
public sealed class TelegramGroupCommands
{
    public const string LinkPrefix = "gl:";
    public const string MutePrefix = "gm:";

    public static readonly IReadOnlySet<string> Commands =
        new HashSet<string>(StringComparer.Ordinal) { "ulash", "sozlash", "uzish" };

    /// <summary>O'chirgich guruhlari — profildagi bilan bir xil (TG3), obuna guruhi guruh chatiga bormaydi.</summary>
    private static readonly (string Key, NotificationType[] Types)[] MuteGroups =
    [
        (MuteKeys.Warehouse, [NotificationType.LowStock, NotificationType.BatchExpiring, NotificationType.BatchExpired]),
        (MuteKeys.Transfers, [NotificationType.TransferPending, NotificationType.TransferConfirmed, NotificationType.TransferRejected, NotificationType.ReturnReceived]),
        (MuteKeys.Production, [NotificationType.ProductionPending, NotificationType.ProductionStarted, NotificationType.ProductionCompleted]),
    ];

    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramChatContext _chats;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramGroupCommands> _logger;

    public TelegramGroupCommands(IServiceProvider services, ITelegramService telegram, TelegramChatContext chats,
        IOptions<TelegramOptions> options, ILogger<TelegramGroupCommands> logger)
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
        if (update.FromId is not { } fromId) return;

        switch (update.Command)
        {
            case "ulash":
                await LinkAsync(update.ChatId, fromId, null, lang, ct);
                break;
            case "sozlash":
                await SettingsAsync(update.ChatId, fromId, lang, null, ct);
                break;
            case "uzish":
                await UnlinkAsync(update.ChatId, fromId, lang, ct);
                break;
            default:
                break;
        }
    }

    public async Task HandleCallbackAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);
        string data = update.CallbackData ?? string.Empty;
        if (update.FromId is not { } fromId || update.CallbackId is null) return;

        if (data.StartsWith(LinkPrefix, StringComparison.Ordinal) && Guid.TryParseExact(data[3..], "N", out Guid tenantId))
        {
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, null, false, ct);
            if (update.MessageId is { } mid) await _telegram.RemoveReplyMarkupAsync(update.ChatId, mid, ct);
            await LinkAsync(update.ChatId, fromId, tenantId, lang, ct);
            return;
        }

        if (data.StartsWith(MutePrefix, StringComparison.Ordinal) && int.TryParse(data[3..], out int index))
        {
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, null, false, ct);
            await SettingsAsync(update.ChatId, fromId, lang, index, ct, update.MessageId);
        }
    }

    /// <summary>Bot guruhdan chiqarildi yoki guruh 403 berdi.</summary>
    public async Task DeactivateAsync(long chatId, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        int changed = await db.TelegramGroups.Where(g => g.ChatId == chatId && g.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, false).SetProperty(g => g.UpdatedAt, DateTime.UtcNow), ct);
        if (changed > 0) _logger.LogInformation("Telegram: guruh {ChatId} uzildi", chatId);
    }

    /// <summary>Yozgan odam qaysi tenant(lar)da admin — shaxsiy ulanish + <c>settings.modules</c>.</summary>
    private async Task<List<TelegramConnection>> AdminConnectionsAsync(long fromId, CancellationToken ct)
    {
        List<TelegramConnection> all = await _chats.ConnectionsAsync(fromId, ct);
        List<TelegramConnection> admins = [];
        foreach (TelegramConnection c in all)
        {
            await using AsyncServiceScope scope = _services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(c.TenantId, c.TenantCode);
            WmsAccess? access = await scope.ServiceProvider.GetRequiredService<IWmsAccessResolver>().ResolveAsync(c.IdentitySub, c.TenantId, ct);
            if (access is not null && access.Permissions.Contains(WmsPermissions.SettingsModules)) admins.Add(c);
        }
        return admins;
    }

    private async Task LinkAsync(long chatId, long fromId, Guid? tenantId, string lang, CancellationToken ct)
    {
        List<TelegramConnection> admins = await AdminConnectionsAsync(fromId, ct);
        if (admins.Count == 0)
        {
            await SendAsync(chatId, Translations.Format(GroupKeys.AdminOnly, lang), ct);
            return;
        }

        TelegramConnection? chosen = tenantId is { } id ? admins.FirstOrDefault(a => a.TenantId == id) : (admins.Count == 1 ? admins[0] : null);
        if (chosen is null)
        {
            var rows = admins.Select(a => new[] { new { text = a.TenantName, callback_data = LinkPrefix + a.TenantId.ToString("N") } }).ToArray();
            await _telegram.SendMessageAsync(chatId, Translations.Format(QueryKeys.WhichTenant, lang),
                JsonSerializer.Serialize(new { inline_keyboard = rows }), ct);
            return;
        }

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        TelegramGroup? group = await db.TelegramGroups.FirstOrDefaultAsync(g => g.ChatId == chatId, ct);
        if (group is null)
        {
            group = new TelegramGroup { ChatId = chatId };
            db.TelegramGroups.Add(group);
        }

        group.TenantId = chosen.TenantId;
        group.LinkedByProfileId = chosen.ProfileId;
        group.IsActive = true;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Telegram: guruh {ChatId} tenant {TenantCode} ga ulandi", chatId, chosen.TenantCode);
        await SendAsync(chatId, Translations.Format(GroupKeys.Linked, lang, WebUtility.HtmlEncode(chosen.TenantName)), ct);
    }

    private async Task UnlinkAsync(long chatId, long fromId, string lang, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        TelegramGroup? group = await db.TelegramGroups.FirstOrDefaultAsync(g => g.ChatId == chatId && g.IsActive, ct);
        if (group is null)
        {
            await SendAsync(chatId, Translations.Format(GroupKeys.NotLinked, lang), ct);
            return;
        }

        List<TelegramConnection> admins = await AdminConnectionsAsync(fromId, ct);
        if (admins.All(a => a.TenantId != group.TenantId))
        {
            await SendAsync(chatId, Translations.Format(GroupKeys.AdminOnly, lang), ct);
            return;
        }

        group.IsActive = false;
        await db.SaveChangesAsync(ct);
        await SendAsync(chatId, Translations.Format(GroupKeys.Unlinked, lang), ct);
    }

    /// <summary><c>/sozlash</c>: guruh turlari tugmalari; <paramref name="toggleIndex"/> — bosilgan guruhni almashtiradi.</summary>
    private async Task SettingsAsync(long chatId, long fromId, string lang, int? toggleIndex, CancellationToken ct, long? editMessageId = null)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        TelegramGroup? group = await db.TelegramGroups.FirstOrDefaultAsync(g => g.ChatId == chatId && g.IsActive, ct);
        if (group is null)
        {
            await SendAsync(chatId, Translations.Format(GroupKeys.NotLinked, lang), ct);
            return;
        }

        List<TelegramConnection> admins = await AdminConnectionsAsync(fromId, ct);
        if (admins.All(a => a.TenantId != group.TenantId))
        {
            await SendAsync(chatId, Translations.Format(GroupKeys.AdminOnly, lang), ct);
            return;
        }

        HashSet<string> muted = new(group.MutedTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [], StringComparer.Ordinal);
        if (toggleIndex is { } idx && idx >= 0 && idx < MuteGroups.Length)
        {
            string[] names = MuteGroups[idx].Types.Select(t => t.ToString()).ToArray();
            bool on = !names.Any(muted.Contains);
            if (on) foreach (string n in names) muted.Add(n);
            else foreach (string n in names) muted.Remove(n);
            group.MutedTypes = muted.Count == 0 ? null : string.Join(',', muted);
            await db.SaveChangesAsync(ct);
        }

        var rows = MuteGroups.Select((g, i) =>
        {
            bool on = !g.Types.Any(t => muted.Contains(t.ToString()));
            return new[] { new { text = (on ? "✅ " : "☐ ") + Translations.Format(g.Key, lang), callback_data = MutePrefix + i } };
        }).ToArray();
        string markup = JsonSerializer.Serialize(new { inline_keyboard = rows });
        string text = Translations.Format(GroupKeys.SettingsTitle, lang);

        if (editMessageId is { } mid)
        {
            // Bosilgan xabarning o'zini yangilaymiz — har bosishda yangi xabar shovqin.
            await _telegram.EditMessageTextAsync(chatId, mid, text, ct);
            await _telegram.SendMessageAsync(chatId, text, markup, ct);
            return;
        }

        await _telegram.SendMessageAsync(chatId, text, markup, ct);
    }

    private async Task SendAsync(long chatId, string text, CancellationToken ct)
    {
        TelegramSendResult result = await _telegram.SendMessageAsync(chatId, text, null, ct);
        if (!result.Ok)
            _logger.LogInformation("Telegram: guruh javobi yuborilmadi (HTTP {StatusCode}: {Description})", result.StatusCode, result.Description);
    }
}

/// <summary>Guruh matn kalitlari (Translations).</summary>
public static class GroupKeys
{
    public const string AdminOnly = "Only an administrator with a connected profile can do this (WMS → Settings → Profile → Connect Telegram).";
    public const string Linked = "✅ This group is now connected to <b>{0}</b>. Notifications will arrive here. /sozlash — choose types, /uzish — disconnect.";
    public const string Unlinked = "This group has been disconnected.";
    public const string NotLinked = "This group is not connected. An administrator can connect it with /ulash.";
    public const string SettingsTitle = "Which notifications should come to this group?";
    public const string ConnectProfileFirst = "Connect your own profile first: WMS → Settings → Profile → Connect Telegram.";
}

/// <summary>O'chirgich guruhi nomlari — profildagi bilan bir xil ma'no.</summary>
public static class MuteKeys
{
    public const string Warehouse = "Stock and batches";
    public const string Transfers = "Transfers";
    public const string Production = "Production";
}
