using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

/// <summary>
/// Platform control plane. All endpoints are SuperAdmin-only and operate cross-tenant.
/// </summary>
[Route("api/admin")]
[Authorize(Policy = "SuperAdmin")]
public class AdminController : BaseController
{
    private readonly ITenantService _tenants;
    private readonly IPlanService _plans;

    public AdminController(ITenantService tenants, IPlanService plans)
    {
        _tenants = tenants;
        _plans = plans;
    }

    // ── Tenants ──────────────────────────────────────────────────────────

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
        => Ok(ApiResponse<List<TenantDto>>.Ok(await _tenants.GetAllAsync()));

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.CreateAsync(dto)));

    [HttpPut("tenants/{id}")]
    public async Task<IActionResult> UpdateTenant(int id, [FromBody] UpdateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.UpdateAsync(id, dto)));

    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DeleteTenant(int id)
    { await _tenants.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpPut("tenants/{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(int id)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.SuspendAsync(id)));

    [HttpPut("tenants/{id}/activate")]
    public async Task<IActionResult> ActivateTenant(int id)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.ActivateAsync(id)));

    [HttpPut("tenants/{id}/plan")]
    public async Task<IActionResult> AssignPlan(int id, [FromBody] AssignPlanDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.AssignPlanAsync(id, dto.PlanId)));

    [HttpGet("tenants/{id}/modules")]
    public async Task<IActionResult> GetTenantModules(int id)
        => Ok(ApiResponse<List<TenantModuleDto>>.Ok(await _tenants.GetModulesAsync(id)));

    [HttpPut("tenants/{id}/modules")]
    public async Task<IActionResult> ToggleTenantModules(int id, [FromBody] ToggleModulesRequest request)
    {
        if (request.Modules is { Count: > 0 })
            await _tenants.ToggleModulesAsync(id, request.Modules);
        else if (request.ModuleId > 0)
            await _tenants.ToggleModuleAsync(id, new ToggleModuleDto { ModuleId = request.ModuleId, IsEnabled = request.IsEnabled });
        return Ok(ApiResponse<object>.Ok(null!, "Updated"));
    }

    // ── Plans ────────────────────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
        => Ok(ApiResponse<List<PlanDto>>.Ok(await _plans.GetPlansAsync()));

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto)
        => Ok(ApiResponse<PlanDto>.Ok(await _plans.CreatePlanAsync(dto)));

    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] CreatePlanDto dto)
        => Ok(ApiResponse<PlanDto>.Ok(await _plans.UpdatePlanAsync(id, dto)));

    [HttpDelete("plans/{id}")]
    public async Task<IActionResult> DeletePlan(int id)
    { await _plans.DeletePlanAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Modules & stats ──────────────────────────────────────────────────

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules()
    {
        var modules = await _tenants.GetModulesAsync(TenantId); // module catalog is global
        var infos = modules.Select(m => new ModuleInfoDto
        {
            ModuleId = m.ModuleId, ModuleName = m.ModuleName, ModuleCode = m.ModuleCode
        }).ToList();
        return Ok(ApiResponse<List<ModuleInfoDto>>.Ok(infos));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(ApiResponse<PlatformStatsDto>.Ok(await _tenants.GetStatsAsync()));
}
