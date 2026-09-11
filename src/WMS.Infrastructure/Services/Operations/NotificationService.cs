using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Services.Operations.Telegram;

namespace WMS.Infrastructure.Services.Operations;

// F6: tenant joriy kontekstdan (so'rovda — token, fon vazifasida — TenantScopes), `userId` —
// `user_profile.id`. Metod nomlari eski: katalog, trade va ishlab chiqarish modullari shularni chaqiradi.
public class NotificationService : INotificationService
{
    private readonly WmsDbContext _db;
    private readonly ITelegramService _telegram;
    private readonly ITenantStateService _tenantState;
    private readonly IRequestLanguage _language;
    private readonly TelegramOptions _telegramOptions;

    public NotificationService(WmsDbContext db, ITelegramService telegram, ITenantStateService tenantState,
        IRequestLanguage language, IOptions<TelegramOptions> telegramOptions)
    {
        ArgumentNullException.ThrowIfNull(telegramOptions);
        _db = db;
        _telegram = telegram;
        _tenantState = tenantState;
        _language = language;
        _telegramOptions = telegramOptions.Value;
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, bool unreadOnly = false)
    {
        var q = _db.Notifications
            .Where(n => n.UserId == null || n.UserId == userId);

        if (unreadOnly)
            q = q.Where(n => !n.IsRead);

        var rows = await q.OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        // Sarlavha va matn so'rov tilida (TG4): eski qatorlarda shablon yo'q — saqlangan matn.
        string lang = _language.Current;
        return rows.Select(n => new NotificationDto
        {
            Id = n.Id,
            Title = Translations.Format(n.Title, lang),
            Message = n.MessageTemplate is { } template
                ? Translations.Format(template, lang, TelegramOutboxComposer.Args(n.MessageArgs))
                : n.Message,
            Type = n.Type,
            EntityType = n.EntityType, EntityId = n.EntityId,
            IsRead = n.IsRead, ReadAt = n.ReadAt, CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _db.Notifications
            .Where(n => (n.UserId == null || n.UserId == userId) && !n.IsRead)
            .CountAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x =>
            x.Id == id && (x.UserId == null || x.UserId == userId));
        if (n == null) return false;
        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        // Bitta UPDATE: SQLite davrida hamma o'qilmagan qator xotiraga olinib birma-bir
        // yozilardi. ExecuteUpdate SaveChanges'ni chetlab o'tadi — UpdatedAt shu yerda qo'yiladi;
        // tenant filtri va RLS esa UPDATE'ga ham tushadi.
        DateTime now = DateTime.UtcNow;
        DateTime? readAt = now;
        await _db.Notifications
            .Where(n => (n.UserId == null || n.UserId == userId) && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, readAt)
                .SetProperty(n => n.UpdatedAt, now));
        return true;
    }

    public Task<Notification> CreateAsync(Guid? userId, string title, string message,
        NotificationType type, string? entityType = null, Guid? entityId = null)
        => SaveAsync(new Notification
        {
            UserId = userId,
            Title = title, Message = message, Type = type,
            EntityType = entityType, EntityId = entityId
        });

    public Task<Notification> NotifyAsync(Guid? userId, string titleKey, string messageTemplate, string?[] messageArgs,
        NotificationType type, string? entityType = null, Guid? entityId = null)
    {
        ArgumentNullException.ThrowIfNull(messageArgs);
        return SaveAsync(new Notification
        {
            UserId = userId,
            Title = titleKey,
            // Inglizcha tayyor matn ham saqlanadi — eski o'quvchilar (eksport, jurnal) shablonni bilmaydi.
            Message = Translations.Format(messageTemplate, Lang.En, messageArgs),
            MessageTemplate = messageTemplate,
            MessageArgs = JsonSerializer.Serialize(messageArgs),
            Type = type,
            EntityType = entityType, EntityId = entityId
        });
    }

    private async Task<Notification> SaveAsync(Notification notification)
    {
        _db.Notifications.Add(notification);

        // Telegram — navbatga, bildirishnoma bilan BITTA SaveChanges'da. HTTP so'rov ichida yo'q:
        // ilgari har ulangan foydalanuvchi uchun ketma-ket (10 s gacha) kutilardi va transfer
        // tasdig'i shuncha osilardi. Yuborish — TelegramOutboxBackgroundService.
        if (_telegram.IsEnabled)
        {
            await EnqueueTelegramAsync(notification);
            await EnqueueRemoveButtonsAsync(notification);
        }

        await _db.SaveChangesAsync();
        return notification;
    }

    /// <summary>
    /// Qabul qiluvchilar (TG3): faol ulanish + faol profil; umumiy bildirishnomada — turga mos
    /// ruxsati borlar (<see cref="NotificationRouting.RequiredPermission"/>); shu turni o'chirmaganlar.
    /// Ilova ichidagi bildirishnoma o'zgarmaydi — bu faqat Telegram kanalining filtri.
    /// </summary>
    private async Task EnqueueTelegramAsync(Notification notification)
    {
        var q = _db.TelegramLinks.AsNoTracking()
            .Where(l => l.IsActive && l.UserProfileId != null && l.UserProfile!.IsActive);

        if (notification.UserId is { } userId)
        {
            q = q.Where(l => l.UserProfileId == userId);
        }
        else if (NotificationRouting.RequiredPermission(notification.Type) is { } permission)
        {
            // Rol → ruxsat: RBAC bazasidan (tokenda ruxsat yo'q), o'chirilgan rol/biriktirish filtrda.
            q = q.Where(l => _db.UserRoles.Any(ur => ur.UserId == l.UserProfileId
                && ur.Role.RolePermissions.Any(rp => rp.PermissionCode == permission)));
        }

        var links = await q.Select(l => new { l.Id, l.ChatId, l.Lang, l.MutedTypes }).ToListAsync();
        if (links.Count == 0) return;

        string typeName = notification.Type.ToString();
        Guid? tenantId = _db.CurrentTenantId;
        string? tenantName = tenantId is { } id ? (await _tenantState.GetAsync(id))?.Name : null;
        string? link = TelegramOutboxComposer.Link(_telegramOptions.WebUrl, notification);

        foreach (var l in links.DistinctBy(l => l.ChatId))
        {
            if (l.MutedTypes is { } muted
                && muted.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains(typeName, StringComparer.OrdinalIgnoreCase))
                continue;

            string text = TelegramOutboxComposer.Text(tenantName, notification, l.Lang, link);
            string? markup = TelegramOutboxComposer.ReplyMarkup(notification, l.Lang, link);
            TelegramOutbox row = TelegramOutboxComposer.Row(notification, tenantId, l.Id, l.ChatId, text, markup);

            // Tinch soatlar (TG11): shoshilinch bo'lmagan xabar ertalabgacha ushlab turiladi — kechasi
            // 02:00 dagi «bosqich tugadi» xabaridan foydalanuvchi botni bloklaydi.
            row.NextAttemptAt = TelegramQuietHours.HoldUntil(DateTime.UtcNow, notification.Type, _telegramOptions);
            _db.TelegramOutboxes.Add(row);
        }
    }

    /// <summary>
    /// Amal bajarildi (tasdiq, rad, boshlash) — shu entity uchun yuborilgan tugmali xabarlarning tugmalari
    /// olib tashlanadi (TG9), web'dan ham, bot orqali ham. Navbat orqali: so'rov ichida HTTP yo'q.
    /// </summary>
    private async Task EnqueueRemoveButtonsAsync(Notification notification)
    {
        if (notification.EntityId is not { } entityId || NotificationRouting.ClosesPending(notification.Type) is not { } pendingType)
            return;

        List<Guid> pendingIds = await _db.Notifications.AsNoTracking()
            .Where(n => n.Type == pendingType && n.EntityId == entityId)
            .Select(n => n.Id)
            .ToListAsync();
        if (pendingIds.Count == 0) return;

        List<TelegramOutbox> sent = await _db.TelegramOutboxes.AsNoTracking()
            .Where(o => o.NotificationId != null && pendingIds.Contains(o.NotificationId.Value)
                && o.Kind == TelegramOutboxKind.Message && o.Status == TelegramOutboxStatus.Sent && o.MessageId != null)
            .ToListAsync();

        foreach (TelegramOutbox row in sent)
            _db.TelegramOutboxes.Add(TelegramOutboxComposer.RemoveButtonsRow(row));
    }
}
