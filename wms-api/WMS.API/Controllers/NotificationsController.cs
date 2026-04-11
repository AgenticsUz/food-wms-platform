using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

public class NotificationsController : BaseController
{
    private readonly INotificationService _notifications;
    public NotificationsController(INotificationService notifications) => _notifications = notifications;

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
        await _notifications.MarkAsReadAsync(id, TenantId);
        return Ok(ApiResponse<object>.Ok(null!, "Marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(TenantId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "All marked as read"));
    }
}
