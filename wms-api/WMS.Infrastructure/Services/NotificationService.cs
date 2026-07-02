using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly WmsDbContext _db;
    private readonly ITelegramService _telegram;
    public NotificationService(WmsDbContext db, ITelegramService telegram)
    { _db = db; _telegram = telegram; }

    public async Task<List<NotificationDto>> GetNotificationsAsync(int tenantId, int userId, bool unreadOnly = false)
    {
        var q = _db.Notifications
            .Where(n => n.TenantId == tenantId && (n.UserId == null || n.UserId == userId))
            .AsQueryable();

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

    public async Task<int> GetUnreadCountAsync(int tenantId, int userId)
    {
        return await _db.Notifications
            .Where(n => n.TenantId == tenantId && (n.UserId == null || n.UserId == userId) && !n.IsRead)
            .CountAsync();
    }

    public async Task<bool> MarkAsReadAsync(int id, int tenantId, int userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x =>
            x.Id == id && x.TenantId == tenantId && (x.UserId == null || x.UserId == userId));
        if (n == null) return false;
        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int tenantId, int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.TenantId == tenantId && (n.UserId == null || n.UserId == userId) && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Notification> CreateAsync(int tenantId, int? userId, string title, string message,
        NotificationType type, string? entityType = null, int? entityId = null)
    {
        var notification = new Notification
        {
            TenantId = tenantId, UserId = userId,
            Title = title, Message = message, Type = type,
            EntityType = entityType, EntityId = entityId
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // Telegram forward — faqat bot sozlangan bo'lsa (default holatda umuman ishlamaydi)
        if (_telegram.IsEnabled)
            await ForwardToTelegram(tenantId, userId, title, message);

        return notification;
    }

    private async Task ForwardToTelegram(int tenantId, int? userId, string title, string message)
    {
        try
        {
            var q = _db.Users.Where(u => u.TenantId == tenantId && u.IsActive && u.TelegramChatId != null);
            if (userId != null) q = q.Where(u => u.Id == userId.Value);
            var chatIds = await q.Select(u => u.TelegramChatId!).Distinct().ToListAsync();

            var text = $"<b>{title}</b>\n{message}";
            foreach (var chatId in chatIds)
                await _telegram.SendMessageAsync(chatId, text);
        }
        catch
        {
            // Best-effort — bildirishnoma yaratish buzilmasin
        }
    }
}
