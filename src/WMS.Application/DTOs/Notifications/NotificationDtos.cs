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

/// <summary><c>GET /api/me/telegram</c>: profil sahifasi ulanish holatini ko'rsatadi.</summary>
/// <remarks>
/// SQLite davrida <c>PUT /api/auth/telegram</c> + qo'lda chat ID edi; F6 da <c>/api/me/telegram</c>
/// ga ko'chdi, Telegram bosqichida (TG1) deep-link'ka o'tdi — chat ID maydoni yo'q.
/// </remarks>
public class TelegramStatusDto
{
    /// <summary>Bot sozlanganmi — sozlanmagan bo'lsa ulash tugmasi ko'rsatilmaydi.</summary>
    public bool Enabled { get; set; }

    /// <summary><c>@</c> siz (<c>AgenticsWmsBot</c>); bot o'chiq bo'lsa <see langword="null"/>.</summary>
    public string? BotUsername { get; set; }

    public bool Linked { get; set; }
    public DateTime? LinkedAt { get; set; }

    /// <summary>Ulangan Telegram akkauntining username'i — «qaysi akkaunt» ko'rinsin.</summary>
    public string? Username { get; set; }

    /// <summary>O'chirilgan bildirishnoma turlari (TG3).</summary>
    public string[] MutedTypes { get; set; } = [];
}

/// <summary><c>POST /api/me/telegram/link-token</c>: havola va uning muddati.</summary>
public class TelegramLinkTokenDto
{
    /// <summary><c>https://t.me/&lt;bot&gt;?start=&lt;token&gt;</c>.</summary>
    public string Url { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
}
