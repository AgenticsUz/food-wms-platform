using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[RequirePermission("settings.modules")]
public class TenantsController : BaseController
{
    // The seeded "WMS Admin" tenant (id 1) is the platform owner; only its users
    // may manage other tenants. Regular tenants can only touch their own modules.
    private const int SystemTenantId = 1;

    private readonly ITenantService _tenants;
    public TenantsController(ITenantService tenants) => _tenants = tenants;

    private bool IsSystemTenant => TenantId == SystemTenantId;

    private IActionResult Forbidden()
        => StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<object>.Fail("Only the system tenant can manage other tenants"));

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsSystemTenant) return Forbidden();
        return Ok(ApiResponse<List<TenantDto>>.Ok(await _tenants.GetAllAsync()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto)
    {
        if (!IsSystemTenant) return Forbidden();
        return Ok(ApiResponse<TenantDto>.Ok(await _tenants.CreateAsync(dto)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantDto dto)
    {
        if (!IsSystemTenant) return Forbidden();
        return Ok(ApiResponse<TenantDto>.Ok(await _tenants.UpdateAsync(id, dto)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsSystemTenant) return Forbidden();
        await _tenants.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted"));
    }

    [HttpGet("{id}/modules")]
    public async Task<IActionResult> GetModules(int id)
    {
        if (id != TenantId && !IsSystemTenant) return Forbidden();
        return Ok(ApiResponse<List<TenantModuleDto>>.Ok(await _tenants.GetModulesAsync(id)));
    }

    [HttpPut("{id}/modules")]
    public async Task<IActionResult> ToggleModules(int id, [FromBody] ToggleModulesRequest request)
    {
        if (id != TenantId && !IsSystemTenant) return Forbidden();

        if (request.Modules is { Count: > 0 })
            await _tenants.ToggleModulesAsync(id, request.Modules);
        else if (request.ModuleId > 0)
            await _tenants.ToggleModuleAsync(id, new ToggleModuleDto { ModuleId = request.ModuleId, IsEnabled = request.IsEnabled });
        return Ok(ApiResponse<object>.Ok(null!, "Updated"));
    }
}
