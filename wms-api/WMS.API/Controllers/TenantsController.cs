using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

public class TenantsController : BaseController
{
    private readonly ITenantService _tenants;
    public TenantsController(ITenantService tenants) => _tenants = tenants;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<TenantDto>>.Ok(await _tenants.GetAllAsync()));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.CreateAsync(dto)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.UpdateAsync(id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _tenants.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id}/modules")]
    public async Task<IActionResult> GetModules(int id)
        => Ok(ApiResponse<List<TenantModuleDto>>.Ok(await _tenants.GetModulesAsync(id)));

    [HttpPut("{id}/modules")]
    public async Task<IActionResult> ToggleModules(int id, [FromBody] List<ToggleModuleDto> modules)
    { await _tenants.ToggleModulesAsync(id, modules); return Ok(ApiResponse<object>.Ok(null!, "Updated")); }
}
