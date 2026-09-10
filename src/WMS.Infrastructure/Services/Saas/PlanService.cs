using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Plans;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
/// <remarks>
/// F6 (D6): plan MODUL bermaydi — <c>ModuleCodes</c> va uning tenantlarga «qo'llanishi»
/// (<c>PlanModules.ApplyPlanModulesAsync</c>) o'chdi. Plan = narx, limitlar, feature to'plami.
/// </remarks>
public class PlanService : IPlanService
{
    private const int CodeMaxLength = 32;
    private const int NameMaxLength = 100;

    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;

    public PlanService(WmsDbContext db, ITenantStateService tenantState)
    {
        _db = db;
        _tenantState = tenantState;
    }

    public async Task<List<PlanDto>> GetPlansAsync(CancellationToken ct = default)
    {
        var plans = await _db.Plans.AsNoTracking().OrderBy(p => p.Price).ThenBy(p => p.Code).ToListAsync(ct);

        var counts = await _db.Tenants
            .Where(t => t.PlanId != null)
            .GroupBy(t => t.PlanId!.Value)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count, ct);

        return plans.Select(p => MapToDto(p, counts.GetValueOrDefault(p.Id, 0))).ToList();
    }

    public async Task<PlanDto> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var (name, code) = ValidateBasics(dto);
        if (await _db.Plans.AnyAsync(p => p.Code == code, ct))
            throw new AppException("A plan with this code already exists");

        var plan = new Plan
        {
            Name = name,
            Code = code,
            Price = dto.Price,
            IsActive = dto.IsActive,
            FeatureCodes = PlanModules.Join(await ValidateFeatureCodesAsync(dto.FeatureCodes, current: null, ct)),
            MaxUsers = dto.MaxUsers,
            MaxWarehouses = dto.MaxWarehouses,
            MaxTransfersPerMonth = dto.MaxTransfersPerMonth,
            TrialDays = dto.TrialDays,
            IsDefault = dto.IsDefault
        };
        _db.Plans.Add(plan);
        await EnsureSingleDefaultAsync(plan, ct);
        await _db.SaveChangesAsync(ct);
        return MapToDto(plan, 0);
    }

    public async Task<PlanDto> UpdatePlanAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Plan not found");

        var (name, code) = ValidateBasics(dto);
        if (code != plan.Code && await _db.Plans.AnyAsync(p => p.Code == code && p.Id != id, ct))
            throw new AppException("A plan with this code already exists");

        plan.Name = name;
        plan.Code = code;
        plan.Price = dto.Price;
        plan.IsActive = dto.IsActive;
        plan.FeatureCodes = PlanModules.Join(await ValidateFeatureCodesAsync(dto.FeatureCodes, plan.FeatureCodes, ct));
        plan.MaxUsers = dto.MaxUsers;
        plan.MaxWarehouses = dto.MaxWarehouses;
        plan.MaxTransfersPerMonth = dto.MaxTransfersPerMonth;
        plan.TrialDays = dto.TrialDays;
        plan.IsDefault = dto.IsDefault;
        await EnsureSingleDefaultAsync(plan, ct);
        await _db.SaveChangesAsync(ct);

        // Planning feature to'plami o'zgargan bo'lishi mumkin — shu plandagi har tenantning keshi
        // bekor, aks holda yangi feature bir daqiqagacha ko'rinmay, operator «ishlamadi» deb o'ylardi.
        var tenantIds = await _db.Tenants.Where(t => t.PlanId == id).Select(t => t.Id).ToListAsync(ct);
        foreach (var tenantId in tenantIds) _tenantState.Invalidate(tenantId);

        return MapToDto(plan, tenantIds.Count);
    }

    public async Task DeletePlanAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Plan not found");
        if (await _db.Tenants.AnyAsync(t => t.PlanId == id, ct))
            throw new AppException("This plan is still assigned to one or more tenants");

        if (plan.IsDefault)
            throw new AppException("This is the default plan for new tenants — make another plan default first");

        plan.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    private static (string Name, string Code) ValidateBasics(CreatePlanDto dto)
    {
        var name = (dto.Name ?? "").Trim();
        var code = (dto.Code ?? "").Trim();
        if (name.Length == 0) throw new AppException("Plan name is required");
        if (code.Length == 0) throw new AppException("Plan code is required");

        // Ustun uzunligi bazada ham cheklangan — tekshirilmasa operator 500 olardi.
        if (name.Length > NameMaxLength) throw new AppException("Plan name must be at most {0} characters", NameMaxLength);
        if (code.Length > CodeMaxLength) throw new AppException("Plan code must be at most {0} characters", CodeMaxLength);
        if (dto.Price < 0 || dto.TrialDays < 0 || dto.MaxUsers < 0 || dto.MaxWarehouses < 0 || dto.MaxTransfersPerMonth < 0)
            throw new AppException("Plan price, trial days and limits cannot be negative");

        return (name, code);
    }

    /// Yangi default plan paydo bo'lsa qolganlaridan bayroq olinadi: JIT yangi tenantga
    /// `IsDefault` planni oladi va ikkitasi bo'lsa qaysi biri tushishi tasodif bo'lardi.
    private async Task EnsureSingleDefaultAsync(Plan plan, CancellationToken ct)
    {
        if (!plan.IsDefault) return;

        var others = await _db.Plans.Where(p => p.IsDefault && p.Id != plan.Id).ToListAsync(ct);
        foreach (var other in others) other.IsDefault = false;
    }

    /// <summary>
    /// Bo'sh ro'yxat: yangi planda — barcha oddiy feature'lar (SQLite davrida planning modullaridan
    /// hisoblanardi; modul endi Identity'da va <c>FeatureResolver</c> modul o'chiq feature'ni baribir
    /// o'chiradi), tahrirlashda — mavjud to'plam saqlanadi. A custom feature can never be part of a
    /// plan: it is written for one customer and is granted per tenant, otherwise "custom" quietly
    /// becomes "standard".
    /// </summary>
    private async Task<List<string>> ValidateFeatureCodesAsync(List<string>? requested, string? current, CancellationToken ct)
    {
        var features = await _db.Features.AsNoTracking().ToListAsync(ct);
        if (features.Count == 0) return new List<string>();

        if (requested is not { Count: > 0 })
        {
            return current is not null
                ? PlanModules.Split(current)
                : features.Where(f => !f.IsCustom).Select(f => f.Code).ToList();
        }

        var known = features.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var code in requested.Select(c => c?.Trim()).Where(c => !string.IsNullOrEmpty(c)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!known.TryGetValue(code!, out var feature))
                throw new AppException(Messages.UnknownFeatureCode, code!);
            if (feature.IsCustom)
                throw new AppException(Messages.CustomFeatureInPlan, code!);
            result.Add(feature.Code);
        }
        return result;
    }

    internal static PlanDto MapToDto(Plan p, int tenantCount) => new()
    {
        Id = p.Id, Name = p.Name, Code = p.Code, Price = p.Price, IsActive = p.IsActive,
        FeatureCodes = PlanModules.Split(p.FeatureCodes),
        MaxUsers = p.MaxUsers, MaxWarehouses = p.MaxWarehouses,
        MaxTransfersPerMonth = p.MaxTransfersPerMonth, TenantCount = tenantCount,
        TrialDays = p.TrialDays, IsDefault = p.IsDefault
    };
}
