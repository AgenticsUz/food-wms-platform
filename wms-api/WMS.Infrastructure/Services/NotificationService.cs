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
    public NotificationService(WmsDbContext db) => _db = db;

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

    public async Task<bool> MarkAsReadAsync(int id, int tenantId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
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
        return notification;
    }
}
