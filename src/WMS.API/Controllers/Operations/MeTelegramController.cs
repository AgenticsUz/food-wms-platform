using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Operations;

/// <summary>
/// Profildagi Telegram ulanishi: <c>GET /api/me/telegram</c>, <c>POST …/link-token</c>, <c>DELETE</c>.
/// </summary>
/// <remarks>
/// SQLite davrida <c>PUT /api/auth/telegram</c> + qo'lda chat ID edi; F6 da <c>/api/me</c> ostiga ko'chdi.
/// TG1: bot foydalanuvchiga u <c>/start</c> bosmaguncha yoza olmaydi (403) — shuning uchun qo'lda
/// chat ID o'rniga deep-link: bu yerdan bir martalik havola, bot <c>/start</c> da o'zi ulaydi.
/// Ruxsat talabi yo'q: har kim faqat O'Z profilini (<c>UserId</c>) boshqaradi.
/// </remarks>
[Route("api/me/telegram")]
public sealed class MeTelegramController : BaseController
{
    private readonly ITelegramLinkService _links;
    public MeTelegramController(ITelegramLinkService links) => _links = links;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(ApiResponse<TelegramStatusDto>.Ok(await _links.GetStatusAsync(UserId, cancellationToken)));

    /// <summary>Havola 10 daqiqa (sozlama) yashaydi, bir marta ishlaydi; oldingi faol havola bekor bo'ladi.</summary>
    [HttpPost("link-token")]
    public async Task<IActionResult> CreateLinkToken(CancellationToken cancellationToken)
        => Ok(ApiResponse<TelegramLinkTokenDto>.Ok(await _links.CreateLinkTokenAsync(UserId, cancellationToken)));

    [HttpDelete]
    public async Task<IActionResult> Unlink(CancellationToken cancellationToken)
    {
        await _links.UnlinkAsync(UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Telegram disconnected"));
    }

    /// <summary>O'chirilgan turlar ro'yxati (TG3) — to'plamni almashtiradi; bo'sh — hammasi yoqiq.</summary>
    [HttpPut("muted")]
    public async Task<IActionResult> SetMuted([FromBody] SetTelegramMutedDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _links.SetMutedTypesAsync(UserId, dto.Types ?? [], cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Saved"));
    }

    /// <summary>Kunlik xulosa (TG11).</summary>
    [HttpPut("digest")]
    public async Task<IActionResult> SetDigest([FromBody] SetTelegramDigestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _links.SetDigestAsync(UserId, dto.Enabled, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Saved"));
    }
}
