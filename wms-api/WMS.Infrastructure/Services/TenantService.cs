using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

using WMS.Application.Common;

namespace WMS.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly WmsDbContext _db;
    public TenantService(WmsDbContext db) => _db = db;

    public async Task<List<TenantDto>> GetAllAsync()
    {
        var userCounts = await _db.Users
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var tenants = await _db.Tenants.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tenants.Select(t => MapToDto(t, userCounts.GetValueOrDefault(t.Id, 0))).ToList();
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        // To'liq provizatsiya: tenant + modullar + Admin rol + admin foydalanuvchi
        var (tenant, _) = await TenantProvisioner.ProvisionAsync(
            _db, dto.Name, dto.Slug, dto.AdminFullName, dto.AdminPhone, dto.AdminPassword);
        return MapToDto(tenant, 1);
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");

        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (slug != tenant.Slug && await _db.Tenants.AnyAsync(t => t.Slug == slug && t.Id != id))
            throw new AppException("This slug is already taken");

        tenant.Name = dto.Name.Trim();
        tenant.Slug = slug;
        tenant.IsActive = dto.IsActive;
        if (dto.PlanType != null) tenant.PlanType = dto.PlanType;
        if (dto.SubscriptionStatus.HasValue) tenant.SubscriptionStatus = dto.SubscriptionStatus.Value;
        await _db.SaveChangesAsync();

        var userCount = await _db.Users.CountAsync(u => u.TenantId == id);
        return MapToDto(tenant, userCount);
    }

    private static TenantDto MapToDto(Tenant t, int userCount) => new()
    {
        Id = t.Id, Name = t.Name, Slug = t.Slug, IsActive = t.IsActive,
        PlanType = t.PlanType, SubscriptionStatus = t.SubscriptionStatus,
        CreatedAt = t.CreatedAt, UserCount = userCount
    };

    public async Task DeleteAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
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
            await ToggleModuleAsync(tenantId, dto);
    }

    public async Task ToggleModuleAsync(int tenantId, ToggleModuleDto dto)
    {
        var moduleExists = await _db.Modules.AnyAsync(m => m.Id == dto.ModuleId);
        if (!moduleExists) return;

        var tm = await _db.TenantModules
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ModuleId == dto.ModuleId);
        if (tm != null)
        {
            tm.IsEnabled = dto.IsEnabled;
        }
        else if (dto.IsEnabled)
        {
            _db.TenantModules.Add(new TenantModule
                { TenantId = tenantId, ModuleId = dto.ModuleId, IsEnabled = true });
        }
        await _db.SaveChangesAsync();
    }
}
