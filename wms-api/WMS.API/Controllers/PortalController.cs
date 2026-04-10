using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.DTOs.Portal;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/portal")]
public class PortalController : ControllerBase
{
    private readonly IPortalAuthService _portal;
    public PortalController(IPortalAuthService portal) => _portal = portal;

    private int CounterpartyId => int.Parse(User.FindFirst("counterpartyId")?.Value ?? "0");
    private int PortalTenantId => int.Parse(User.FindFirst("tenantId")?.Value ?? "0");

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] PortalLoginDto dto)
    {
        try
        {
            var result = await _portal.LoginAsync(dto);
            return Ok(ApiResponse<PortalAuthResponseDto>.Ok(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
        => Ok(ApiResponse<PortalCounterpartyDto>.Ok(await _portal.GetCurrentAsync(CounterpartyId)));

    [HttpGet("transfers")]
    [Authorize]
    public async Task<IActionResult> GetTransfers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(ApiResponse<List<TransferDto>>.Ok(
            await _portal.GetTransfersAsync(CounterpartyId, PortalTenantId, page, pageSize)));

    [HttpGet("transfers/{id}")]
    [Authorize]
    public async Task<IActionResult> GetTransfer(int id)
        => Ok(ApiResponse<TransferDto>.Ok(
            await _portal.GetTransferByIdAsync(CounterpartyId, PortalTenantId, id)));

    [HttpGet("finance")]
    [Authorize]
    public async Task<IActionResult> GetFinance()
        => Ok(ApiResponse<PortalFinanceDto>.Ok(
            await _portal.GetFinanceAsync(CounterpartyId, PortalTenantId)));

    [HttpGet("payments")]
    [Authorize]
    public async Task<IActionResult> GetPayments()
        => Ok(ApiResponse<List<PaymentHistoryDto>>.Ok(
            await _portal.GetPaymentsAsync(CounterpartyId, PortalTenantId)));
}
