using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly WmsDbContext _db;
    public TenantService(WmsDbContext db) => _db = db;

    public async Task<List<TenantDto>> GetAllAsync()
    {
        return await _db.Tenants.Select(t => new TenantDto
        {
            Id = t.Id, Name = t.Name, Slug = t.Slug, IsActive = t.IsActive
        }).ToListAsync();
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        var tenant = new Tenant { Name = dto.Name, Slug = dto.Slug };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return new TenantDto { Id = tenant.Id, Name = tenant.Name, Slug = tenant.Slug, IsActive = tenant.IsActive };
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new Exception("Tenant not found");
        tenant.Name = dto.Name;
        tenant.Slug = dto.Slug;
        tenant.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return new TenantDto { Id = tenant.Id, Name = tenant.Name, Slug = tenant.Slug, IsActive = tenant.IsActive };
    }

    public async Task DeleteAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new Exception("Tenant not found");
        tenant.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<TenantModuleDto>> GetModulesAsync(int tenantId)
    {
        var allModules = await _db.Modules.OrderBy(m => m.OrderNumber).ToListAsync();
        var tenantModules = await _db.TenantModules
            .Where(tm => tm.TenantId == tenantId).ToListAsync();

        return allModules.Select(m =>
        {
            var tm = tenantModules.FirstOrDefault(x => x.ModuleId == m.Id);
            return new TenantModuleDto
            {
                ModuleId = m.Id, ModuleName = m.Name, ModuleCode = m.Code,
                IsEnabled = tm?.IsEnabled ?? false
            };
        }).ToList();
    }

    public async Task ToggleModulesAsync(int tenantId, List<ToggleModuleDto> modules)
    {
        foreach (var dto in modules)
        {
            var tm = await _db.TenantModules
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ModuleId == dto.ModuleId);
            if (tm == null)
            {
                _db.TenantModules.Add(new TenantModule
                    { TenantId = tenantId, ModuleId = dto.ModuleId, IsEnabled = dto.IsEnabled });
            }
            else
            {
                tm.IsEnabled = dto.IsEnabled;
            }
        }
        await _db.SaveChangesAsync();
    }
}
