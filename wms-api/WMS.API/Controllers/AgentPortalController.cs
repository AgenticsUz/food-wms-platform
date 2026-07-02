using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/agent-portal")]
[Authorize(Policy = "AgentPortalOnly")]
public class AgentPortalController : ControllerBase
{
    private readonly IAgentService _agents;
    public AgentPortalController(IAgentService agents) => _agents = agents;

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
}
