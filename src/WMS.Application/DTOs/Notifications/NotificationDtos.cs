using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

/// <summary>
/// <c>PUT /api/me/telegram</c> tanasi. SQLite davrida <c>PUT /api/auth/telegram</c> edi
/// (<c>DTOs/Auth</c> da) — o'z login'i o'chgach profil yuzasi <c>/api/me</c> ga ko'chdi.
/// </summary>
public class SetTelegramDto
{
    /// <summary>Bo'sh yoki <see langword="null"/> — ulanishni uzadi.</summary>
    public string? ChatId { get; set; }
}

/// <summary><c>GET /api/me/telegram</c>: profil sahifasi joriy qiymatni ko'rsatadi.</summary>
public class TelegramLinkDto
{
    public string? ChatId { get; set; }

    /// <summary>Bot sozlanganmi — sozlanmagan bo'lsa ulash foydasiz, frontend ogohlantiradi.</summary>
    public bool Enabled { get; set; }
}
