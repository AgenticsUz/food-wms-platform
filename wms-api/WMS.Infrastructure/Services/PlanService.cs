using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class PlanService : IPlanService
{
    private readonly WmsDbContext _db;
    public PlanService(WmsDbContext db) => _db = db;

    public async Task<List<PlanDto>> GetPlansAsync()
    {
        var plans = await _db.Plans.OrderBy(p => p.Price).ToListAsync();

        var counts = await _db.Tenants
            .Where(t => t.PlanId != null)
            .GroupBy(t => t.PlanId!.Value)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        return plans.Select(p => MapToDto(p, counts.GetValueOrDefault(p.Id, 0))).ToList();
    }

    public async Task<PlanDto> CreatePlanAsync(CreatePlanDto dto)
    {
        var code = (dto.Code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new AppException("Plan name is required");
        if (string.IsNullOrWhiteSpace(code)) throw new AppException("Plan code is required");
        if (await _db.Plans.AnyAsync(p => p.Code == code))
            throw new AppException("A plan with this code already exists");

        var plan = new Plan
        {
            Name = dto.Name.Trim(),
            Code = code,
            Price = dto.Price,
            IsActive = dto.IsActive,
            ModuleCodes = PlanModules.Join(dto.ModuleCodes),
            MaxUsers = dto.MaxUsers,
            MaxWarehouses = dto.MaxWarehouses,
            MaxTransfersPerMonth = dto.MaxTransfersPerMonth
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();
        return MapToDto(plan, 0);
    }

    public async Task<PlanDto> UpdatePlanAsync(int id, CreatePlanDto dto)
    {
        var plan = await _db.Plans.FindAsync(id) ?? throw new NotFoundException("Plan not found");

        var code = (dto.Code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new AppException("Plan name is required");
        if (string.IsNullOrWhiteSpace(code)) throw new AppException("Plan code is required");
        if (code != plan.Code && await _db.Plans.AnyAsync(p => p.Code == code && p.Id != id))
            throw new AppException("A plan with this code already exists");

        plan.Name = dto.Name.Trim();
        plan.Code = code;
        plan.Price = dto.Price;
        plan.IsActive = dto.IsActive;
        plan.ModuleCodes = PlanModules.Join(dto.ModuleCodes);
        plan.MaxUsers = dto.MaxUsers;
        plan.MaxWarehouses = dto.MaxWarehouses;
        plan.MaxTransfersPerMonth = dto.MaxTransfersPerMonth;
        await _db.SaveChangesAsync();

        var count = await _db.Tenants.CountAsync(t => t.PlanId == id);
        return MapToDto(plan, count);
    }

    public async Task DeletePlanAsync(int id)
    {
        var plan = await _db.Plans.FindAsync(id) ?? throw new NotFoundException("Plan not found");
        if (await _db.Tenants.AnyAsync(t => t.PlanId == id))
            throw new AppException("This plan is still assigned to one or more tenants");

        plan.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private static PlanDto MapToDto(Plan p, int tenantCount) => new()
    {
        Id = p.Id, Name = p.Name, Code = p.Code, Price = p.Price, IsActive = p.IsActive,
        ModuleCodes = PlanModules.Split(p.ModuleCodes),
        MaxUsers = p.MaxUsers, MaxWarehouses = p.MaxWarehouses,
        MaxTransfersPerMonth = p.MaxTransfersPerMonth, TenantCount = tenantCount
    };
}
