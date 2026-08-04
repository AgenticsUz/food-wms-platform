using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.DTOs.Portal;
using WMS.Application.DTOs.Transfers;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Policy = "PortalOnly")]
public class PortalController : ControllerBase
{
    private readonly IPortalAuthService _portal;
    private readonly ILeadService _leads;
    public PortalController(IPortalAuthService portal, ILeadService leads)
    { _portal = portal; _leads = leads; }

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
        catch (AppException ex) when (ex is PaymentRequiredException
                                      or ModuleDisabledException
                                      or FeatureDisabledException)
        {
            // Entitlement and subscription refusals carry their own status and code —
            // let the exception middleware map them instead of flattening to 400.
            throw;
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
        => Ok(ApiResponse<PortalCounterpartyDto>.Ok(await _portal.GetCurrentAsync(CounterpartyId, PortalTenantId)));

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

    // ── "I want my own system" (S7) ──
    // Suppliers and clients already using a customer's portal are the warmest leads there
    // are: they have seen the product work. The request is credited to the tenant whose
    // portal they came from (ReferrerTenantId).
    //
    // These endpoints are deliberately NOT exempt from subscription enforcement: if the
    // host tenant is suspended, its portal stops too.

    [HttpPost("upgrade-interest")]
    [Authorize]
    public async Task<IActionResult> SubmitUpgradeInterest([FromBody] UpgradeInterestDto dto)
    {
        var me = await _portal.GetCurrentAsync(CounterpartyId, PortalTenantId);
        var lead = await _leads.SubmitPortalInterestAsync(
            PortalTenantId, CounterpartyId, null, me.Name, me.Name,
            new UpgradeInterestDto { Phone = dto.Phone, Note = dto.Note });
        return Ok(ApiResponse<LeadDto>.Ok(lead, "Thank you — we will contact you shortly"));
    }

    [HttpGet("upgrade-interest")]
    [Authorize]
    public async Task<IActionResult> GetUpgradeInterest()
        => Ok(ApiResponse<UpgradeInterestStatusDto>.Ok(
            await _leads.GetPortalInterestAsync(PortalTenantId, CounterpartyId, null)));
}
