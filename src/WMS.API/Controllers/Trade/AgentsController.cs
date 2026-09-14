using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
using WMS.Application.DTOs.Portal;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Trade;

[Route("api/agents")]
[RequirePermission(WmsPermissions.AgentsView)]
[RequireModule(ModuleCodes.Agents)]
public class AgentsController : BaseController
{
    private readonly IAgentService _agents;
    public AgentsController(IAgentService agents) => _agents = agents;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<AgentDto>>.Ok(await _agents.GetAllAsync()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<AgentDto>.Ok(await _agents.GetByIdAsync(id)));

    [HttpPost]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> Create([FromBody] CreateAgentDto dto)
        => Ok(ApiResponse<AgentDto>.Ok(await _agents.CreateAsync(dto)));

    [HttpPut("{id}")]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentDto dto)
        => Ok(ApiResponse<AgentDto>.Ok(await _agents.UpdateAsync(id, dto)));

    [HttpDelete("{id}")]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _agents.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Kabinet hisobi (F9) — kontragentdagi bilan bir xil naqsh ──

    [HttpGet("{id:guid}/portal-account")]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> GetPortalAccount(Guid id, [FromServices] IPortalAccountService portal, CancellationToken ct)
        => Ok(ApiResponse<PortalAccountDto>.Ok(await portal.GetAgentAccountAsync(id, ct)));

    [HttpPut("{id:guid}/portal-account")]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> LinkPortalAccount(Guid id, [FromBody] LinkPortalAccountDto dto,
        [FromServices] IPortalAccountService portal, CancellationToken ct)
        => Ok(ApiResponse<PortalAccountDto>.Ok(await portal.LinkAgentAsync(id, dto.IdentitySub, ct), "Portal access granted"));

    [HttpDelete("{id:guid}/portal-account")]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> UnlinkPortalAccount(Guid id, [FromServices] IPortalAccountService portal, CancellationToken ct)
        => Ok(ApiResponse<PortalAccountDto>.Ok(await portal.UnlinkAgentAsync(id, ct), "Portal access revoked"));

    [HttpGet("{id}/sales")]
    public async Task<IActionResult> GetSales(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<AgentSalesReportDto>.Ok(await _agents.GetSalesReportAsync(id, from, to)));

    [HttpGet("{id}/commissions")]
    [RequireFeature(FeatureCodes.AgentsCommissions)]
    public async Task<IActionResult> GetCommissions(Guid id)
        => Ok(ApiResponse<List<CommissionRecordDto>>.Ok(await _agents.GetCommissionsAsync(id)));

    [HttpPost("{id}/commissions/pay")]
    [RequireFeature(FeatureCodes.AgentsCommissions)]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> PayCommission(Guid id, [FromBody] PayCommissionDto dto)
    {
        await _agents.PayCommissionAsync(UserId, id, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Commission paid"));
    }

    [HttpPut("commissions/{recordId}/status")]
    [RequireFeature(FeatureCodes.AgentsCommissions)]
    [RequirePermission(WmsPermissions.AgentsManage)]
    public async Task<IActionResult> UpdateCommissionStatus(Guid recordId, [FromBody] UpdateCommissionStatusDto dto)
    {
        await _agents.UpdateCommissionStatusAsync(recordId, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Status updated"));
    }
}
