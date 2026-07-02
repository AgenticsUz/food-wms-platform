using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

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
            await _notifications.GetNotificationsAsync(TenantId, UserId, unreadOnly)));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
        => Ok(ApiResponse<UnreadCountDto>.Ok(
            new UnreadCountDto { Count = await _notifications.GetUnreadCountAsync(TenantId, UserId) }));

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notifications.MarkAsReadAsync(id, TenantId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(TenantId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "All marked as read"));
    }

    [HttpPost("check-expiring-batches")]
    public async Task<IActionResult> CheckExpiringBatches([FromQuery] int warningDaysAhead = 3)
    {
        var created = await _batchExpiry.CheckBatchesAsync(TenantId, warningDaysAhead);
        return Ok(ApiResponse<object>.Ok(new { created }, $"{created} notification(s) created"));
    }
}
