using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[Route("api/agents")]
public class AgentsController : BaseController
{
    private readonly IAgentService _agents;
    public AgentsController(IAgentService agents) => _agents = agents;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<AgentDto>>.Ok(await _agents.GetAllAsync(TenantId)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(ApiResponse<AgentDto>.Ok(await _agents.GetByIdAsync(TenantId, id)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentDto dto)
        => Ok(ApiResponse<AgentDto>.Ok(await _agents.CreateAsync(TenantId, dto)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAgentDto dto)
        => Ok(ApiResponse<AgentDto>.Ok(await _agents.UpdateAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _agents.DeleteAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id}/sales")]
    public async Task<IActionResult> GetSales(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<AgentSalesReportDto>.Ok(await _agents.GetSalesReportAsync(TenantId, id, from, to)));

    [HttpGet("{id}/commissions")]
    public async Task<IActionResult> GetCommissions(int id)
        => Ok(ApiResponse<List<CommissionRecordDto>>.Ok(await _agents.GetCommissionsAsync(TenantId, id)));

    [HttpPost("{id}/commissions/pay")]
    public async Task<IActionResult> PayCommission(int id, [FromBody] PayCommissionDto dto)
    {
        await _agents.PayCommissionAsync(TenantId, UserId, id, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Commission paid"));
    }

    [HttpPut("commissions/{recordId}/status")]
    public async Task<IActionResult> UpdateCommissionStatus(int recordId, [FromBody] UpdateCommissionStatusDto dto)
    {
        await _agents.UpdateCommissionStatusAsync(TenantId, recordId, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Status updated"));
    }
}
