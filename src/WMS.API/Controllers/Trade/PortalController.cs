using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Portal;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Trade;

/// <summary>
/// Kabinet (F9): mijoz, ta'minotchi va agent O'Z oldi-berdisini ko'radi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <see cref="BaseController"/> dan meros OLINMAYDI: uning <c>[Route("api/[controller]")]</c>
/// i bu yerga to'g'ri kelmaydi va kabinet ruxsat kodlariga tayanmaydi. Lekin
/// <c>[RequireTenant]</c> SHART — tenantsiz so'rov RLS'siz ketardi.
/// </para>
/// <para>
/// ⚠️ Bu yerda <c>[RequirePermission]</c> YO'Q va bo'lmaydi: kabinet rollarining
/// (<c>client</c>, <c>agent</c>) WMS ruxsati ataylab BO'SH. Eshikni servis ochadi — u
/// tokendagi <c>sub</c> ni kartaga bog'lay olmasa 403 beradi (fail-closed), ya'ni
/// «rol bor, karta yo'q» holatida hech narsa ko'rinmaydi.
/// </para>
/// <para>
/// ⚠️ Hujjat id'si yo'lda keladi, KONTRAGENT id'si esa hech qachon: u tokendan
/// yechiladi. Shuning uchun «boshqasining id'sini qo'yib ko'raman» hujumi uchun yuza yo'q.
/// </para>
/// </remarks>
[ApiController]
[Route("api/portal")]
[Authorize]
[RequireTenant]
public sealed class PortalController : ControllerBase
{
    private readonly IPortalService _portal;

    public PortalController(IPortalService portal) => _portal = portal;

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<PortalMeDto>>> Me(CancellationToken ct)
        => Ok(ApiResponse<PortalMeDto>.Ok(await _portal.GetMeAsync(ct)));

    [HttpGet("finance")]
    public async Task<ActionResult<ApiResponse<PortalFinanceDto>>> Finance(CancellationToken ct)
        => Ok(ApiResponse<PortalFinanceDto>.Ok(await _portal.GetFinanceAsync(ct)));

    [HttpGet("transfers")]
    public async Task<ActionResult<ApiResponse<List<PortalTransferDto>>>> Transfers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(ApiResponse<List<PortalTransferDto>>.Ok(await _portal.GetTransfersAsync(page, pageSize, ct)));

    [HttpGet("transfers/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PortalTransferDto>>> Transfer(Guid id, CancellationToken ct)
        => Ok(ApiResponse<PortalTransferDto>.Ok(await _portal.GetTransferAsync(id, ct)));

    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<List<PortalPaymentDto>>>> Payments(CancellationToken ct)
        => Ok(ApiResponse<List<PortalPaymentDto>>.Ok(await _portal.GetPaymentsAsync(ct)));

    [HttpGet("agent/summary")]
    public async Task<ActionResult<ApiResponse<PortalAgentSummaryDto>>> AgentSummary(CancellationToken ct)
        => Ok(ApiResponse<PortalAgentSummaryDto>.Ok(await _portal.GetAgentSummaryAsync(ct)));

    [HttpGet("agent/clients")]
    public async Task<ActionResult<ApiResponse<List<PortalAgentClientDto>>>> AgentClients(CancellationToken ct)
        => Ok(ApiResponse<List<PortalAgentClientDto>>.Ok(await _portal.GetAgentClientsAsync(ct)));
}
