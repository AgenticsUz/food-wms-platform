using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Qc;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/qc")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission(WmsPermissions.QualityView)]
[RequireModule(ModuleCodes.Quality)]
public class QcController : BaseController
{
    private readonly IQcService _qc;
    public QcController(IQcService qc) => _qc = qc;

    [HttpGet("parameters")]
    [RequireFeature(FeatureCodes.QcParameters)]
    public async Task<IActionResult> GetParameters()
        => Ok(ApiResponse<List<QcParameterDto>>.Ok(await _qc.GetParametersAsync()));

    [HttpPost("parameters")]
    [RequireFeature(FeatureCodes.QcParameters)]
    [RequirePermission(WmsPermissions.QualityManage)]
    public async Task<IActionResult> CreateParameter([FromBody] CreateQcParameterDto dto)
        => Ok(ApiResponse<QcParameterDto>.Ok(await _qc.CreateParameterAsync(dto)));

    [HttpPut("parameters/{id}")]
    [RequireFeature(FeatureCodes.QcParameters)]
    [RequirePermission(WmsPermissions.QualityManage)]
    public async Task<IActionResult> UpdateParameter(Guid id, [FromBody] CreateQcParameterDto dto)
        => Ok(ApiResponse<QcParameterDto>.Ok(await _qc.UpdateParameterAsync(id, dto)));

    [HttpDelete("parameters/{id}")]
    [RequireFeature(FeatureCodes.QcParameters)]
    [RequirePermission(WmsPermissions.QualityManage)]
    public async Task<IActionResult> DeleteParameter(Guid id)
    { await _qc.DeleteParameterAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("checks")]
    [RequireFeature(FeatureCodes.QcChecks)]
    public async Task<IActionResult> GetChecks([FromQuery] Guid? transferId, [FromQuery] Guid? stageExecutionId)
        => Ok(ApiResponse<List<QcCheckDto>>.Ok(await _qc.GetChecksAsync(transferId, stageExecutionId)));

    [HttpPost("checks")]
    [RequireFeature(FeatureCodes.QcChecks)]
    [RequirePermission(WmsPermissions.QualityManage)]
    public async Task<IActionResult> CreateCheck([FromBody] CreateQcCheckDto dto)
        => Ok(ApiResponse<QcCheckDto>.Ok(await _qc.CreateCheckAsync(UserId, dto)));
}
