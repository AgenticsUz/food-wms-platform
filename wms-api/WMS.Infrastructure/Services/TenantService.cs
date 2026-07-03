using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

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

        var planNames = await _db.Plans
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var tenants = await _db.Tenants.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tenants.Select(t => MapToDto(t, userCounts.GetValueOrDefault(t.Id, 0),
            t.PlanId != null ? planNames.GetValueOrDefault(t.PlanId.Value) : null)).ToList();
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        // To'liq provizatsiya: tenant + modullar + Admin rol + admin foydalanuvchi
        var (tenant, _) = await TenantProvisioner.ProvisionAsync(
            _db, dto.Name, dto.Slug, dto.AdminFullName, dto.AdminPhone, dto.AdminPassword);

        string? planName = null;
        if (dto.PlanId.HasValue)
        {
            var plan = await _db.Plans.FindAsync(dto.PlanId.Value)
                ?? throw new NotFoundException("Plan not found");
            tenant.PlanId = plan.Id;
            tenant.PlanType = plan.Code;
            await _db.SaveChangesAsync();
            await PlanModules.ApplyPlanModulesAsync(_db, tenant.Id, plan);
            planName = plan.Name;
        }

        return MapToDto(tenant, 1, planName);
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
        tenant.TrialEndsAt = dto.TrialEndsAt;

        Plan? plan = null;
        var planChanged = dto.PlanId != tenant.PlanId;
        if (dto.PlanId.HasValue)
        {
            plan = await _db.Plans.FindAsync(dto.PlanId.Value)
                ?? throw new NotFoundException("Plan not found");
            tenant.PlanId = plan.Id;
            tenant.PlanType = plan.Code;
        }
        else
        {
            tenant.PlanId = null;
        }
        await _db.SaveChangesAsync();

        if (planChanged && plan != null)
            await PlanModules.ApplyPlanModulesAsync(_db, tenant.Id, plan);

        var userCount = await _db.Users.CountAsync(u => u.TenantId == id);
        var planName = tenant.PlanId != null
            ? (plan?.Name ?? (await _db.Plans.FindAsync(tenant.PlanId.Value))?.Name)
            : null;
        return MapToDto(tenant, userCount, planName);
    }

    private static TenantDto MapToDto(Tenant t, int userCount, string? planName) => new()
    {
        Id = t.Id, Name = t.Name, Slug = t.Slug, IsActive = t.IsActive,
        PlanType = t.PlanType, SubscriptionStatus = t.SubscriptionStatus,
        CreatedAt = t.CreatedAt, UserCount = userCount,
        PlanId = t.PlanId, PlanName = planName, TrialEndsAt = t.TrialEndsAt
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

    // ── Control plane (platform admin) ──────────────────────────────────────

    public async Task<PlatformStatsDto> GetStatsAsync()
    {
        var tenants = await _db.Tenants.ToListAsync();
        var totalUsers = await _db.Users.CountAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 12-month growth, oldest → newest.
        var growth = new List<MonthCountDto>();
        for (var i = 11; i >= 0; i--)
        {
            var start = monthStart.AddMonths(-i);
            var end = start.AddMonths(1);
            growth.Add(new MonthCountDto
            {
                Month = start.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture),
                Count = tenants.Count(t => t.CreatedAt >= start && t.CreatedAt < end)
            });
        }

        var recent = tenants
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentTenantDto
            {
                Id = t.Id, Name = t.Name, Slug = t.Slug,
                SubscriptionStatus = t.SubscriptionStatus, CreatedAt = t.CreatedAt
            })
            .ToList();

        return new PlatformStatsDto
        {
            TotalTenants = tenants.Count,
            ActiveTenants = tenants.Count(t => t.SubscriptionStatus == SubscriptionStatus.Active),
            TrialTenants = tenants.Count(t => t.SubscriptionStatus == SubscriptionStatus.Trial),
            SuspendedTenants = tenants.Count(t => t.SubscriptionStatus == SubscriptionStatus.Suspended),
            TotalUsers = totalUsers,
            NewTenantsThisMonth = tenants.Count(t => t.CreatedAt >= monthStart),
            MonthlyGrowth = growth,
            RecentTenants = recent
        };
    }

    public async Task<TenantDto> SuspendAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
        tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
        await _db.SaveChangesAsync();
        return await MapWithCountsAsync(tenant);
    }

    public async Task<TenantDto> ActivateAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
        tenant.SubscriptionStatus = SubscriptionStatus.Active;
        await _db.SaveChangesAsync();
        return await MapWithCountsAsync(tenant);
    }

    public async Task<TenantDto> AssignPlanAsync(int id, int planId)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
        var plan = await _db.Plans.FindAsync(planId) ?? throw new NotFoundException("Plan not found");

        tenant.PlanId = plan.Id;
        tenant.PlanType = plan.Code;
        await _db.SaveChangesAsync();
        await PlanModules.ApplyPlanModulesAsync(_db, tenant.Id, plan);

        var userCount = await _db.Users.CountAsync(u => u.TenantId == id);
        return MapToDto(tenant, userCount, plan.Name);
    }

    private async Task<TenantDto> MapWithCountsAsync(Tenant tenant)
    {
        var userCount = await _db.Users.CountAsync(u => u.TenantId == tenant.Id);
        var planName = tenant.PlanId != null
            ? (await _db.Plans.FindAsync(tenant.PlanId.Value))?.Name
            : null;
        return MapToDto(tenant, userCount, planName);
    }
}
