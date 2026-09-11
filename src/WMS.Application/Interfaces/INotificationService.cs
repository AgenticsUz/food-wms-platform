using WMS.Application.DTOs.Notifications;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

/// <remarks>
/// Katalog (partiya muddati), trade (transfer tasdiq/rad) va ishlab chiqarish modullari
/// <see cref="CreateAsync"/> ni ESKI nomi bilan chaqiradi — F6 da faqat kalitlar Guid bo'ldi va
/// tenant parametri o'chdi (D4: tenant joriy kontekstdan, fon vazifasida <c>TenantScopes</c> dan).
/// Har <c>userId</c> — <c>user_profile.id</c>.
/// </remarks>
public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<bool> MarkAsReadAsync(Guid id, Guid userId);
    Task<bool> MarkAllAsReadAsync(Guid userId);

    /// <param name="userId">Qabul qiluvchi profil; <see langword="null"/> — tenantdagi hammaga.</param>
    /// <remarks>
    /// Bot yoqilgan bo'lsa ulangan qabul qiluvchilar uchun Telegram navbatiga (<c>telegram_outbox</c>)
    /// qator ham yoziladi — bildirishnoma bilan bitta <c>SaveChanges</c> da, HTTP so'rov ichida yo'q.
    /// Ulash/uzish — <see cref="ITelegramLinkService"/>.
    /// </remarks>
    Task<Notification> CreateAsync(Guid? userId, string title, string message,
        NotificationType type, string? entityType = null, Guid? entityId = null);
}
