using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
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
