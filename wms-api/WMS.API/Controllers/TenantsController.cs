using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

public class TenantsController : BaseController
{
    private readonly ITenantService _tenants;
    public TenantsController(ITenantService tenants) => _tenants = tenants;

    // ── Cross-tenant (control plane) — faqat SuperAdmin ──

    [HttpGet]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<TenantDto>>.Ok(await _tenants.GetAllAsync()));

    [HttpPost]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.CreateAsync(dto)));

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.UpdateAsync(id, dto)));

    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    { await _tenants.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Modullar — FAQAT KO'RISH ──
    // Modul to'plamini plan belgilaydi (control plane). Tenant admin o'z modullarini
    // ko'ra oladi, lekin yoqa olmaydi — aks holda plan gating o'z-o'zidan yechiladi.
    // O'zgartirish faqat SuperAdmin uchun: PUT /api/admin/tenants/{id}/modules.

    [HttpGet("{id}/modules")]
    [RequirePermission("settings.modules")]
    public async Task<IActionResult> GetModules(int id)
    {
        if (id != TenantId && !IsSuperAdmin) return Forbidden();
        return Ok(ApiResponse<List<TenantModuleDto>>.Ok(await _tenants.GetModulesAsync(id)));
    }

    [HttpPut("{id}/modules")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> ToggleModules(int id, [FromBody] ToggleModulesRequest request)
    {
        if (request.Modules is { Count: > 0 })
            await _tenants.ToggleModulesAsync(id, request.Modules);
        else if (request.ModuleId > 0)
            await _tenants.ToggleModuleAsync(id, new ToggleModuleDto { ModuleId = request.ModuleId, IsEnabled = request.IsEnabled });
        return Ok(ApiResponse<object>.Ok(null!, "Updated"));
    }

    private IActionResult Forbidden()
        => StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<object>.Fail("You can only view your own tenant's modules"));
}
