using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Kpi;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Operations;

[Route("api/shifts")]
[RequirePermission(WmsPermissions.KpiView)]
[RequireModule(ModuleCodes.Kpi)]
[RequireFeature(FeatureCodes.KpiShifts)]
public class ShiftsController : BaseController
{
    private readonly IKpiService _kpi;
    public ShiftsController(IKpiService kpi) => _kpi = kpi;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<ShiftDto>>.Ok(await _kpi.GetShiftsAsync()));

    [HttpPost]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> Create([FromBody] CreateShiftDto dto)
        => Ok(ApiResponse<ShiftDto>.Ok(await _kpi.CreateShiftAsync(dto)));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShiftDto dto)
        => Ok(ApiResponse<ShiftDto>.Ok(await _kpi.UpdateShiftAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _kpi.DeleteShiftAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}

[Route("api/kpi")]
[RequirePermission(WmsPermissions.KpiView)]
[RequireModule(ModuleCodes.Kpi)]
public class KpiController : BaseController
{
    private readonly IKpiService _kpi;
    public KpiController(IKpiService kpi) => _kpi = kpi;

    [HttpGet("plans")]
    [RequireFeature(FeatureCodes.KpiPlans)]
    public async Task<IActionResult> GetPlans([FromQuery] DateTime? date)
        => Ok(ApiResponse<List<ShiftPlanDto>>.Ok(await _kpi.GetPlansAsync(date)));

    [HttpPost("plans")]
    [RequireFeature(FeatureCodes.KpiPlans)]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> CreatePlan([FromBody] CreateShiftPlanDto dto)
        => Ok(ApiResponse<ShiftPlanDto>.Ok(await _kpi.CreatePlanAsync(dto)));

    [HttpGet("actuals")]
    [RequireFeature(FeatureCodes.KpiPlans)]
    public async Task<IActionResult> GetActuals([FromQuery] DateTime? date)
        => Ok(ApiResponse<List<ShiftActualDto>>.Ok(await _kpi.GetActualsAsync(date)));

    [HttpPost("actuals")]
    [RequireFeature(FeatureCodes.KpiPlans)]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> CreateActual([FromBody] CreateShiftActualDto dto)
        => Ok(ApiResponse<ShiftActualDto>.Ok(await _kpi.CreateActualAsync(dto)));

    [HttpGet("summary")]
    [RequireFeature(FeatureCodes.KpiEfficiency)]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<KpiSummaryDto>.Ok(await _kpi.GetSummaryAsync(from, to)));

    [HttpGet("efficiency")]
    [RequireFeature(FeatureCodes.KpiEfficiency)]
    public async Task<IActionResult> GetEfficiency([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<List<EfficiencyDto>>.Ok(await _kpi.GetEfficiencyAsync(from, to)));
}

[Route("api/attendance")]
[RequirePermission(WmsPermissions.KpiView)]
[RequireModule(ModuleCodes.Kpi)]
[RequireFeature(FeatureCodes.KpiAttendance)]
public class AttendanceController : BaseController
{
    private readonly IKpiService _kpi;
    public AttendanceController(IKpiService kpi) => _kpi = kpi;

    [HttpPost("checkin")]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        => Ok(ApiResponse<AttendanceLogDto>.Ok(await _kpi.CheckInAsync(dto)));

    [HttpPut("{id:guid}/checkout")]
    [RequirePermission(WmsPermissions.KpiManage)]
    public async Task<IActionResult> CheckOut(Guid id)
        => Ok(ApiResponse<AttendanceLogDto>.Ok(await _kpi.CheckOutAsync(id)));

    /// <param name="userId"><c>user_profile.id</c>.</param>
    /// <param name="date">Kun (UTC).</param>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, [FromQuery] DateTime? date)
        => Ok(ApiResponse<List<AttendanceLogDto>>.Ok(await _kpi.GetAttendanceAsync(userId, date)));
}
