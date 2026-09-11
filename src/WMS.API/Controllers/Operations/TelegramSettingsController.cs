using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Operations;

/// <summary>
/// Tenantning Telegram sozlamasi (TG13): mijozlarga xabar yuborish roziligi va qarz eslatmasi oralig'i.
/// <c>settings.modules</c> — tenant egasining qarori: mijozning mijoziga biz xabar yuboramiz.
/// </summary>
[Route("api/settings/telegram-clients")]
[RequirePermission(WmsPermissions.SettingsModules)]
public sealed class TelegramSettingsController : BaseController
{
    private readonly ITelegramLinkService _links;
    public TelegramSettingsController(ITelegramLinkService links) => _links = links;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(ApiResponse<TelegramClientSettingsDto>.Ok(await _links.GetClientSettingsAsync(cancellationToken)));

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] TelegramClientSettingsDto dto, CancellationToken cancellationToken)
    {
        await _links.SetClientSettingsAsync(dto, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Saved"));
    }
}
