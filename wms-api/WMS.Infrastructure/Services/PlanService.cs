using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
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
            FeatureCodes = PlanModules.Join(await ValidateFeatureCodesAsync(dto.FeatureCodes, dto.ModuleCodes)),
            MaxUsers = dto.MaxUsers,
            MaxWarehouses = dto.MaxWarehouses,
            MaxTransfersPerMonth = dto.MaxTransfersPerMonth,
            TrialDays = dto.TrialDays,
            IsDefault = dto.IsDefault
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();
        await EnsureSingleDefaultAsync(plan);
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
        plan.FeatureCodes = PlanModules.Join(await ValidateFeatureCodesAsync(dto.FeatureCodes, dto.ModuleCodes));
        plan.MaxUsers = dto.MaxUsers;
        plan.MaxWarehouses = dto.MaxWarehouses;
        plan.MaxTransfersPerMonth = dto.MaxTransfersPerMonth;
        plan.TrialDays = dto.TrialDays;
        plan.IsDefault = dto.IsDefault;
        await _db.SaveChangesAsync();
        await EnsureSingleDefaultAsync(plan);

        var count = await _db.Tenants.CountAsync(t => t.PlanId == id);
        return MapToDto(plan, count);
    }

    public async Task DeletePlanAsync(int id)
    {
        var plan = await _db.Plans.FindAsync(id) ?? throw new NotFoundException("Plan not found");
        if (await _db.Tenants.AnyAsync(t => t.PlanId == id))
            throw new AppException("This plan is still assigned to one or more tenants");

        if (plan.IsDefault)
            throw new AppException("This is the default registration plan — make another plan default first");

        plan.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    /// Registratsiya uchun default plan bittagina bo'lishi kerak — yangisi qo'yilsa
    /// qolganlaridan bayroq olinadi.
    private async Task EnsureSingleDefaultAsync(Plan plan)
    {
        if (!plan.IsDefault) return;

        var others = await _db.Plans.Where(p => p.IsDefault && p.Id != plan.Id).ToListAsync();
        if (others.Count == 0) return;

        foreach (var other in others) other.IsDefault = false;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Empty list = derive the feature set from the plan's modules (what a plan means by
    /// default). A custom feature can never be part of a plan: it is written for one
    /// customer and is granted per tenant, otherwise "custom" quietly becomes "standard".
    /// </summary>
    private async Task<List<string>> ValidateFeatureCodesAsync(List<string> requested, List<string> moduleCodes)
    {
        var features = await _db.Features.AsNoTracking().ToListAsync();
        if (features.Count == 0) return new List<string>();

        if (requested is not { Count: > 0 })
        {
            var modules = new HashSet<string>(moduleCodes ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            return features
                .Where(f => !f.IsCustom && (f.ModuleCode == null || modules.Contains(f.ModuleCode)))
                .Select(f => f.Code)
                .ToList();
        }

        var known = features.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var code in requested.Select(c => c?.Trim()).Where(c => !string.IsNullOrEmpty(c)))
        {
            if (!known.TryGetValue(code!, out var feature))
                throw new AppException(Messages.UnknownFeatureCode, code!);
            if (feature.IsCustom)
                throw new AppException(Messages.CustomFeatureInPlan, code!);
            result.Add(feature.Code);
        }
        return result;
    }

    private static PlanDto MapToDto(Plan p, int tenantCount) => new()
    {
        Id = p.Id, Name = p.Name, Code = p.Code, Price = p.Price, IsActive = p.IsActive,
        ModuleCodes = PlanModules.Split(p.ModuleCodes),
        FeatureCodes = PlanModules.Split(p.FeatureCodes),
        MaxUsers = p.MaxUsers, MaxWarehouses = p.MaxWarehouses,
        MaxTransfersPerMonth = p.MaxTransfersPerMonth, TenantCount = tenantCount,
        TrialDays = p.TrialDays, IsDefault = p.IsDefault
    };
}
