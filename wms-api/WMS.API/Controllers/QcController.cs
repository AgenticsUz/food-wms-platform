using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Qc;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/qc")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("quality.view")]
public class QcController : BaseController
{
    private readonly IQcService _qc;
    public QcController(IQcService qc) => _qc = qc;

    [HttpGet("parameters")]
    public async Task<IActionResult> GetParameters()
        => Ok(ApiResponse<List<QcParameterDto>>.Ok(await _qc.GetParametersAsync(TenantId)));

    [HttpPost("parameters")]
    [RequirePermission("quality.manage")]
    public async Task<IActionResult> CreateParameter([FromBody] CreateQcParameterDto dto)
        => Ok(ApiResponse<QcParameterDto>.Ok(await _qc.CreateParameterAsync(TenantId, dto)));

    [HttpGet("checks")]
    public async Task<IActionResult> GetChecks([FromQuery] int? transferId, [FromQuery] int? stageExecutionId)
        => Ok(ApiResponse<List<QcCheckDto>>.Ok(await _qc.GetChecksAsync(TenantId, transferId, stageExecutionId)));

    [HttpPost("checks")]
    [RequirePermission("quality.manage")]
    public async Task<IActionResult> CreateCheck([FromBody] CreateQcCheckDto dto)
        => Ok(ApiResponse<QcCheckDto>.Ok(await _qc.CreateCheckAsync(TenantId, UserId, dto)));
}
