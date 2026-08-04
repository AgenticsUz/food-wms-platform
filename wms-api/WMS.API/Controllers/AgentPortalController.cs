using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/agent-portal")]
[Authorize(Policy = "AgentPortalOnly")]
public class AgentPortalController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly ILeadService _leads;
    public AgentPortalController(IAgentService agents, ILeadService leads)
    { _agents = agents; _leads = leads; }

    private int AgentId => int.Parse(User.FindFirst("agentId")?.Value ?? "0");
    private int PortalTenantId => int.Parse(User.FindFirst("tenantId")?.Value ?? "0");

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AgentPortalLoginDto dto)
    {
        try
        {
            var result = await _agents.PortalLoginAsync(dto);
            return Ok(ApiResponse<AgentPortalAuthResponseDto>.Ok(result));
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
        => Ok(ApiResponse<AgentPortalProfileDto>.Ok(await _agents.GetPortalProfileAsync(AgentId, PortalTenantId)));

    [HttpGet("sales")]
    [Authorize]
    public async Task<IActionResult> GetSales()
        => Ok(ApiResponse<AgentSalesReportDto>.Ok(
            await _agents.GetPortalSalesReportAsync(AgentId, PortalTenantId)));

    [HttpGet("commissions")]
    [Authorize]
    public async Task<IActionResult> GetCommissions()
        => Ok(ApiResponse<List<CommissionRecordDto>>.Ok(
            await _agents.GetPortalCommissionsAsync(AgentId, PortalTenantId)));

    // ── "I want my own system" (S7) — same flow as the counterparty portal ──

    [HttpPost("upgrade-interest")]
    [Authorize]
    public async Task<IActionResult> SubmitUpgradeInterest([FromBody] UpgradeInterestDto dto)
    {
        var me = await _agents.GetPortalProfileAsync(AgentId, PortalTenantId);
        var lead = await _leads.SubmitPortalInterestAsync(
            PortalTenantId, null, AgentId, me.Name, me.Name,
            new UpgradeInterestDto { Phone = dto.Phone ?? me.Phone, Note = dto.Note });
        return Ok(ApiResponse<LeadDto>.Ok(lead, "Thank you — we will contact you shortly"));
    }

    [HttpGet("upgrade-interest")]
    [Authorize]
    public async Task<IActionResult> GetUpgradeInterest()
        => Ok(ApiResponse<UpgradeInterestStatusDto>.Ok(
            await _leads.GetPortalInterestAsync(PortalTenantId, null, AgentId)));
}
