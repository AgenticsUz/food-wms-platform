using Microsoft.EntityFrameworkCore;
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

    public NotificationService(WmsDbContext db, ITelegramService telegram)
    { _db = db; _telegram = telegram; }

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, bool unreadOnly = false)
    {
        var q = _db.Notifications
            .Where(n => n.UserId == null || n.UserId == userId);

        if (unreadOnly)
            q = q.Where(n => !n.IsRead);

        return await q.OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationDto
            {
                Id = n.Id, Title = n.Title, Message = n.Message, Type = n.Type,
                EntityType = n.EntityType, EntityId = n.EntityId,
                IsRead = n.IsRead, ReadAt = n.ReadAt, CreatedAt = n.CreatedAt
            }).ToListAsync();
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

    public async Task<Notification> CreateAsync(Guid? userId, string title, string message,
        NotificationType type, string? entityType = null, Guid? entityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title, Message = message, Type = type,
            EntityType = entityType, EntityId = entityId
        };
        _db.Notifications.Add(notification);

        // Telegram — navbatga, bildirishnoma bilan BITTA SaveChanges'da. HTTP so'rov ichida yo'q:
        // ilgari har ulangan foydalanuvchi uchun ketma-ket (10 s gacha) kutilardi va transfer
        // tasdig'i shuncha osilardi. Yuborish — TelegramOutboxBackgroundService.
        if (_telegram.IsEnabled)
            await EnqueueTelegramAsync(notification, userId);

        await _db.SaveChangesAsync();
        return notification;
    }

    /// <summary>Qabul qiluvchilar: faol ulanish + faol profil (+ shu turni o'chirmagan — TG3).</summary>
    private async Task EnqueueTelegramAsync(Notification notification, Guid? userId)
    {
        var q = _db.TelegramLinks.AsNoTracking()
            .Where(l => l.IsActive && l.UserProfileId != null && l.UserProfile!.IsActive);
        if (userId != null) q = q.Where(l => l.UserProfileId == userId.Value);

        var links = await q.Select(l => new { l.Id, l.ChatId, l.MutedTypes }).ToListAsync();
        if (links.Count == 0) return;

        string typeName = notification.Type.ToString();
        string text = TelegramOutboxComposer.Text(notification.Title, notification.Message);

        foreach (var link in links.DistinctBy(l => l.ChatId))
        {
            if (link.MutedTypes is { } muted
                && muted.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains(typeName, StringComparer.OrdinalIgnoreCase))
                continue;

            _db.TelegramOutboxes.Add(TelegramOutboxComposer.Row(notification, _db.CurrentTenantId, link.Id, link.ChatId, text));
        }
    }
}
