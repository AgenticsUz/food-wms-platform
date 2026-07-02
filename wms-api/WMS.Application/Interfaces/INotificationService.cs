using WMS.Application.DTOs.Notifications;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync(int tenantId, int userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(int tenantId, int userId);
    Task<bool> MarkAsReadAsync(int id, int tenantId, int userId);
    Task<bool> MarkAllAsReadAsync(int tenantId, int userId);
    Task<Notification> CreateAsync(int tenantId, int? userId, string title, string message,
        NotificationType type, string? entityType = null, int? entityId = null);
}
