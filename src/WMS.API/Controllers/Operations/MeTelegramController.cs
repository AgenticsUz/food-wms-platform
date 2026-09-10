using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Operations;

/// <summary>
/// Profildagi Telegram ulanishi: <c>GET/PUT /api/me/telegram</c>.
/// </summary>
/// <remarks>
/// SQLite davrida <c>PUT /api/auth/telegram</c> edi va joriy qiymat login javobida
/// (<c>telegramChatId</c>) kelardi. O'z login'i o'chdi (D5), <c>/api/me</c> integratorniki —
/// shuning uchun alohida controller, <c>/api/me</c> ostida. Ruxsat talabi yo'q: har kim faqat
/// O'Z profilini (<c>UserId</c>) o'zgartiradi.
/// </remarks>
[Route("api/me/telegram")]
public sealed class MeTelegramController : BaseController
{
    private readonly INotificationService _notifications;
    public MeTelegramController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(ApiResponse<TelegramLinkDto>.Ok(await _notifications.GetTelegramChatAsync(UserId)));

    [HttpPut]
    public async Task<IActionResult> SetTelegram([FromBody] SetTelegramDto dto)
    {
        await _notifications.SetTelegramChatAsync(UserId, dto.ChatId);
        return Ok(ApiResponse<object>.Ok(null!, "Telegram updated"));
    }
}
