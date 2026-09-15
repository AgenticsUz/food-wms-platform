using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Catalog;

/// <summary>
/// Standart ombor sozlamasi (P2.6): kirim/chiqim formasi omborni so'ramasligi uchun.
/// </summary>
/// <remarks>
/// <para>
/// Ruxsatlar UCH XIL, shuning uchun class darajasida talab YO'Q:
/// o'qish — <c>warehouse.view</c> (formaga kerak), tenant sozlamasini yozish —
/// <c>settings.modules</c> (<c>TelegramSettingsController</c> naqshi, tenant egasining qarori),
/// shaxsiy tanlov — oddiy autentifikatsiya (odam o'z profilini o'zgartiradi).
/// </para>
/// <para>
/// ⚠️ <c>MeController</c> ga tegilmadi (u boshqa agentda): shaxsiy tanlov shu yerda,
/// <c>PUT /api/settings/warehouses/mine</c> da.
/// </para>
/// </remarks>
[Route("api/settings/warehouses")]
[RequireModule(ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished)]
public sealed class WarehouseSettingsController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public WarehouseSettingsController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    [RequirePermission(WmsPermissions.WarehouseView)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(ApiResponse<WarehouseDefaultsDto>.Ok(await _warehouses.GetDefaultsAsync(cancellationToken)));

    [HttpPut]
    [RequirePermission(WmsPermissions.SettingsModules)]
    public async Task<IActionResult> Set([FromBody] WarehouseDefaultsDto dto, CancellationToken cancellationToken)
    {
        await _warehouses.SetDefaultsAsync(dto, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Saved"));
    }

    /// <summary>Shaxsiy standart ombor — tenant sozlamasini bosib ketadi.</summary>
    [HttpPut("mine")]
    public async Task<IActionResult> SetMine([FromBody] MyDefaultWarehouseDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _warehouses.SetMyDefaultWarehouseAsync(dto.WarehouseId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Saved"));
    }
}
