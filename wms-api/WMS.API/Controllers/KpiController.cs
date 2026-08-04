using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Kpi;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/shifts")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("kpi.view")]
[RequireModule(ModuleCodes.Kpi)]
public class ShiftsController : BaseController
{
    private readonly IKpiService _kpi;
    public ShiftsController(IKpiService kpi) => _kpi = kpi;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<ShiftDto>>.Ok(await _kpi.GetShiftsAsync(TenantId)));

    [HttpPost]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> Create([FromBody] CreateShiftDto dto)
        => Ok(ApiResponse<ShiftDto>.Ok(await _kpi.CreateShiftAsync(TenantId, dto)));

    [HttpPut("{id}")]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShiftDto dto)
        => Ok(ApiResponse<ShiftDto>.Ok(await _kpi.UpdateShiftAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> Delete(int id)
    { await _kpi.DeleteShiftAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}

[ApiController]
[Route("api/kpi")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("kpi.view")]
[RequireModule(ModuleCodes.Kpi)]
public class KpiController : BaseController
{
    private readonly IKpiService _kpi;
    public KpiController(IKpiService kpi) => _kpi = kpi;

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans([FromQuery] DateTime? date)
        => Ok(ApiResponse<List<ShiftPlanDto>>.Ok(await _kpi.GetPlansAsync(TenantId, date)));

    [HttpPost("plans")]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateShiftPlanDto dto)
        => Ok(ApiResponse<ShiftPlanDto>.Ok(await _kpi.CreatePlanAsync(TenantId, dto)));

    [HttpGet("actuals")]
    public async Task<IActionResult> GetActuals([FromQuery] DateTime? date)
        => Ok(ApiResponse<List<ShiftActualDto>>.Ok(await _kpi.GetActualsAsync(TenantId, date)));

    [HttpPost("actuals")]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> CreateActual([FromBody] CreateShiftActualDto dto)
        => Ok(ApiResponse<ShiftActualDto>.Ok(await _kpi.CreateActualAsync(TenantId, dto)));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<KpiSummaryDto>.Ok(await _kpi.GetSummaryAsync(TenantId, from, to)));

    [HttpGet("efficiency")]
    public async Task<IActionResult> GetEfficiency([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<List<EfficiencyDto>>.Ok(await _kpi.GetEfficiencyAsync(TenantId, from, to)));
}

[ApiController]
[Route("api/attendance")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("kpi.view")]
[RequireModule(ModuleCodes.Kpi)]
public class AttendanceController : BaseController
{
    private readonly IKpiService _kpi;
    public AttendanceController(IKpiService kpi) => _kpi = kpi;

    [HttpPost("checkin")]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        => Ok(ApiResponse<AttendanceLogDto>.Ok(await _kpi.CheckInAsync(TenantId, dto)));

    [HttpPut("{id}/checkout")]
    [RequirePermission("kpi.manage")]
    public async Task<IActionResult> CheckOut(int id)
        => Ok(ApiResponse<AttendanceLogDto>.Ok(await _kpi.CheckOutAsync(TenantId, id)));

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? userId, [FromQuery] DateTime? date)
        => Ok(ApiResponse<List<AttendanceLogDto>>.Ok(await _kpi.GetAttendanceAsync(TenantId, userId, date)));
}
