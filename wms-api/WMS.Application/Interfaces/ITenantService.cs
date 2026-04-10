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
}
