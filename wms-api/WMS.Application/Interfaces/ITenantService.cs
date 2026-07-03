using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Tenants;

namespace WMS.Application.Interfaces;

public interface ITenantService
{
    Task<List<TenantDto>> GetAllAsync();
    Task<TenantDto> CreateAsync(CreateTenantDto dto);
    Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto);
    Task DeleteAsync(int id);
    Task<List<TenantModuleDto>> GetModulesAsync(int tenantId);
    Task ToggleModulesAsync(int tenantId, List<ToggleModuleDto> modules);
    Task ToggleModuleAsync(int tenantId, ToggleModuleDto dto);

    // Platform-admin (control plane) — operate on any tenant by id, no tenant filter.
    Task<PlatformStatsDto> GetStatsAsync();
    Task<TenantDto> SuspendAsync(int id);
    Task<TenantDto> ActivateAsync(int id);
    Task<TenantDto> AssignPlanAsync(int id, int planId);
}
