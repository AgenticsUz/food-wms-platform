using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Operations;

// Marshrut — BaseController'ning `api/[controller]` i (`/api/notifications`), SQLite davridagidek.
// Ruxsat talabi yo'q: har foydalanuvchi o'z bildirishnomalarini ko'radi.
public class NotificationsController : BaseController
{
    private readonly INotificationService _notifications;
    private readonly IBatchExpiryService _batchExpiry;
    public NotificationsController(INotificationService notifications, IBatchExpiryService batchExpiry)
    {
        _notifications = notifications;
        _batchExpiry = batchExpiry;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool unreadOnly = false)
        => Ok(ApiResponse<List<NotificationDto>>.Ok(
            await _notifications.GetNotificationsAsync(UserId, unreadOnly)));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
        => Ok(ApiResponse<UnreadCountDto>.Ok(
            new UnreadCountDto { Count = await _notifications.GetUnreadCountAsync(UserId) }));

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notifications.MarkAsReadAsync(id, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(UserId);
        return Ok(ApiResponse<object>.Ok(null!, "All marked as read"));
    }

    // IBatchExpiryService — katalog modulining (W1·1) servisi; tenant joriy kontekstdan.
    [HttpPost("check-expiring-batches")]
    public async Task<IActionResult> CheckExpiringBatches([FromQuery] int warningDaysAhead = 3)
    {
        var created = await _batchExpiry.CheckBatchesAsync(warningDaysAhead);
        return Ok(ApiResponse<object>.Ok(new { created }, $"{created} notification(s) created"));
    }
}
