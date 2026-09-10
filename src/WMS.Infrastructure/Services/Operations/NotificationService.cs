using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations;

// F6: tenant joriy kontekstdan (so'rovda — token, fon vazifasida — TenantScopes), `userId` —
// `user_profile.id`. Metod nomlari eski: katalog, trade va ishlab chiqarish modullari shularni chaqiradi.
public class NotificationService : INotificationService
{
    /// <summary><c>user_profile.telegram_chat_id</c> ustunining uzunligi (AccessConfiguration).</summary>
    private const int TelegramChatIdMaxLength = 64;

    private readonly WmsDbContext _db;
    private readonly ITelegramService _telegram;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(WmsDbContext db, ITelegramService telegram, ILogger<NotificationService> logger)
    { _db = db; _telegram = telegram; _logger = logger; }

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
        await _db.SaveChangesAsync();

        // Telegram forward — faqat bot sozlangan bo'lsa (default holatda umuman ishlamaydi)
        if (_telegram.IsEnabled)
            await ForwardToTelegram(userId, title, message);

        return notification;
    }

    public async Task<TelegramLinkDto> GetTelegramChatAsync(Guid userId)
    {
        var chatId = await _db.UserProfiles
            .Where(u => u.Id == userId)
            .Select(u => new { u.TelegramChatId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("User not found");

        return new TelegramLinkDto { ChatId = chatId.TelegramChatId, Enabled = _telegram.IsEnabled };
    }

    public async Task SetTelegramChatAsync(Guid userId, string? chatId)
    {
        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found");

        var value = chatId?.Trim();
        // SQLite uzunlikni tekshirmasdi; Postgres varchar(64) dan oshganini 500 bilan rad etardi.
        if (value is { Length: > TelegramChatIdMaxLength })
            throw new AppException("Telegram chat id is too long");

        user.TelegramChatId = string.IsNullOrWhiteSpace(value) ? null : value;
        await _db.SaveChangesAsync();
    }

    private async Task ForwardToTelegram(Guid? userId, string title, string message)
    {
        try
        {
            var q = _db.UserProfiles.Where(u => u.IsActive && u.TelegramChatId != null);
            if (userId != null) q = q.Where(u => u.Id == userId.Value);
            var chatIds = await q.Select(u => u.TelegramChatId!).Distinct().ToListAsync();

            // parse_mode=HTML: matndagi `<` yoki `&` (mahsulot nomi, izoh) kodlanmasa Telegram
            // butun xabarni 400 bilan rad etadi — SQLite davrida shunday xabarlar jimgina yo'qolardi.
            var text = $"<b>{WebUtility.HtmlEncode(title)}</b>\n{WebUtility.HtmlEncode(message)}";
            foreach (var chatId in chatIds)
                await _telegram.SendMessageAsync(chatId, text);
        }
#pragma warning disable CA1031 // Best-effort — bildirishnoma yaratish (va chaqiruvchi oqim) buzilmasin.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Bildirishnoma Telegram'ga uzatilmadi");
        }
    }
}
