using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    private readonly SubscriptionOptions _subscription;

    public TenantService(WmsDbContext db, ITenantStateService tenantState,
        IOptions<SubscriptionOptions> subscription)
    {
        _db = db;
        _tenantState = tenantState;
        _subscription = subscription.Value;
    }

    public async Task<List<TenantDto>> GetAllAsync()
    {
        var userCounts = await _db.Users
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var planNames = await _db.Plans
            .ToDictionaryAsync(p => p.Id, p => p.Name);
        var inns = await _db.Organizations.ToDictionaryAsync(o => o.Id, o => o.Inn);

        var tenants = await _db.Tenants.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tenants.Select(t => MapToDto(t, userCounts.GetValueOrDefault(t.Id, 0),
            t.PlanId != null ? planNames.GetValueOrDefault(t.PlanId.Value) : null,
            t.OrganizationId != null ? inns.GetValueOrDefault(t.OrganizationId.Value) : null)).ToList();
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        // To'liq provizatsiya: tenant + plan modullari + trial muddati + Admin rol + admin
        // foydalanuvchi. Plan berilmasa platformaning default (trial) plani qo'llanadi.
        var (tenant, _) = await TenantProvisioner.ProvisionAsync(
            _db, dto.Name, dto.Slug, dto.AdminFullName, dto.AdminPhone, dto.AdminPassword,
            _subscription, dto.PlanId, dto.Inn);

        // A tenant is normally created right after a payment, so the console may send the
        // paid-through date with the creation itself — that also ends the default trial.
        if (dto.PaidUntil is { } paidUntil)
        {
            tenant.PaidUntil = paidUntil;
            tenant.SubscriptionStatus = SubscriptionStatus.Active;
            tenant.TrialEndsAt = null;
            await _db.SaveChangesAsync();
        }

        var planName = tenant.PlanId != null
            ? (await _db.Plans.FindAsync(tenant.PlanId.Value))?.Name
            : null;

        return MapToDto(tenant, 1, planName, await GetInnAsync(tenant.OrganizationId));
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
        // Faqat kelgan bo'lsa yoziladi — aks holda formada bu maydon bo'lmasa mavjud
        // trial sanasi jimgina o'chib ketardi.
        if (dto.TrialEndsAt.HasValue) tenant.TrialEndsAt = dto.TrialEndsAt;
        if (dto.PaidUntil.HasValue) tenant.PaidUntil = dto.PaidUntil;

        // Brand colour: a value sets it, an empty string clears it back to the default
        // theme, an omitted field changes nothing.
        if (dto.BrandColor != null)
            tenant.BrandColor = string.IsNullOrWhiteSpace(dto.BrandColor)
                ? null
                : BrandingService.NormalizeColor(dto.BrandColor);

        // INN: a value (re-)links the tenant to its Organization, an empty string unlinks it,
        // null (field omitted) leaves the existing link alone.
        if (dto.Inn != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Inn))
            {
                tenant.OrganizationId = null;
            }
            else
            {
                var organization = await OrganizationMatcher.ResolveAsync(_db, dto.Inn, tenant.Name);
                tenant.OrganizationId = organization?.Id;
            }
        }

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
            if (planChanged) tenant.PlanType = null;
        }

        ApplyStatusRules(tenant, plan, dto.SubscriptionStatus);
        await _db.SaveChangesAsync();

        // Modul to'plami plan bilan birga o'zgaradi. Plan olib tashlanganda eski planning
        // to'plami qolib ketmasin — plansiz tenant "cheklovsiz" deb qaraladi.
        if (planChanged)
        {
            if (plan != null) await PlanModules.ApplyPlanModulesAsync(_db, tenant.Id, plan);
            else await PlanModules.ApplyAllModulesAsync(_db, tenant.Id);
        }

        _tenantState.Invalidate(tenant.Id);

        var userCount = await _db.Users.CountAsync(u => u.TenantId == id);
        var planName = tenant.PlanId != null
            ? (plan?.Name ?? (await _db.Plans.FindAsync(tenant.PlanId.Value))?.Name)
            : null;
        return MapToDto(tenant, userCount, planName, await GetInnAsync(tenant.OrganizationId));
    }

    private async Task<string?> GetInnAsync(int? organizationId)
        => organizationId == null
            ? null
            : await _db.Organizations.Where(o => o.Id == organizationId).Select(o => o.Inn).FirstOrDefaultAsync();

    /// <summary>
    /// Holat mashinasi (avval aniqlanmagan edi — plan biriktirilsa ham status Trial bo'lib
    /// qolishi mumkin edi):
    /// • SuperAdmin statusni aniq yuborgan bo'lsa — o'sha kuch bilan qoladi.
    /// • Pullik plan (Price &gt; 0) biriktirilsa → Active, trial sanasi tozalanadi.
    /// • Trial plan (TrialDays &gt; 0) biriktirilsa va sana yo'q bo'lsa → Trial + sana qo'yiladi.
    /// </summary>
    private void ApplyStatusRules(Tenant tenant, Plan? plan, SubscriptionStatus? explicitStatus)
    {
        if (plan == null || explicitStatus.HasValue) return;

        if (plan.Price > 0)
        {
            if (tenant.SubscriptionStatus == SubscriptionStatus.Trial)
            {
                tenant.SubscriptionStatus = SubscriptionStatus.Active;
                tenant.TrialEndsAt = null;
            }
        }
        else if (plan.TrialDays > 0 && tenant.TrialEndsAt == null)
        {
            tenant.SubscriptionStatus = SubscriptionStatus.Trial;
            tenant.TrialEndsAt = DateTime.UtcNow.AddDays(plan.TrialDays);
        }
    }

    private static TenantDto MapToDto(Tenant t, int userCount, string? planName, string? inn = null) => new()
    {
        Id = t.Id, Name = t.Name, Slug = t.Slug, IsActive = t.IsActive,
        PlanType = t.PlanType, SubscriptionStatus = t.SubscriptionStatus,
        CreatedAt = t.CreatedAt, UserCount = userCount,
        PlanId = t.PlanId, PlanName = planName, TrialEndsAt = t.TrialEndsAt,
        PaidUntil = t.PaidUntil, SuspendReason = t.SuspendReason, SuspendNote = t.SuspendNote,
        SuspendPublicMessage = t.SuspendPublicMessage, SuspendedUntil = t.SuspendedUntil,
        SuspendedAt = t.SuspendedAt,
        Inn = inn, OrganizationId = t.OrganizationId,
        LogoUrl = t.LogoUrl, LogoSquareUrl = t.LogoSquareUrl, BrandColor = t.BrandColor
    };

    public async Task DeleteAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
        tenant.IsDeleted = true;
        await _db.SaveChangesAsync();
        _tenantState.Invalidate(id);
    }

    /// Modul katalogi (platforma darajasida). Avval superadminning O'Z tenanti bo'yicha
    /// olinardi, shuning uchun IsEnabled maydoni ma'nosiz chiqardi.
    public async Task<List<ModuleInfoDto>> GetModuleCatalogAsync()
        => await _db.Modules
            .OrderBy(m => m.OrderNumber)
            .Select(m => new ModuleInfoDto
            {
                ModuleId = m.Id, ModuleName = m.Name, ModuleCode = m.Code
            })
            .ToListAsync();

    public async Task<List<TenantModuleDto>> GetModulesAsync(int tenantId)
    {
        // Tenant yo'q bo'lsa ham javob qaytarish xavfli edi: chaqiruvchi katalogning
        // standart holatini o'sha tenantning haqiqiy sozlamasi deb o'qiydi.
        await EnsureTenantExistsAsync(tenantId);

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

    /// Modul endpointlari uchun — qolgan hammasi tenantni `FindAsync` bilan oladi va shu
    /// xatoni tashlaydi, bular esa umuman tekshirmasdi.
    private async Task EnsureTenantExistsAsync(int tenantId)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId))
            throw new NotFoundException("Tenant not found");
    }

    public async Task ToggleModulesAsync(int tenantId, List<ToggleModuleDto> modules)
    {
        foreach (var dto in modules)
            await ToggleModuleAsync(tenantId, dto);
    }

    public async Task ToggleModuleAsync(int tenantId, ToggleModuleDto dto)
    {
        // Aks holda mavjud bo'lmagan tenant uchun yetim TenantModule qatori yozilardi.
        await EnsureTenantExistsAsync(tenantId);

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
        _tenantState.Invalidate(tenantId);
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

    public async Task<TenantDto> SuspendAsync(int id, SuspendTenantDto? dto = null, int suspendedByUserId = 0)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");

        tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
        tenant.SuspendReason = dto?.Reason ?? SuspendReason.Other;
        tenant.SuspendNote = dto?.Note;
        tenant.SuspendPublicMessage = string.IsNullOrWhiteSpace(dto?.PublicMessage) ? null : dto!.PublicMessage!.Trim();
        tenant.SuspendedUntil = dto?.Until;
        tenant.SuspendedAt = DateTime.UtcNow;
        tenant.SuspendedByUserId = suspendedByUserId > 0 ? suspendedByUserId : null;

        await _db.SaveChangesAsync();
        // Cache tozalanadi — suspend keyingi so'rovdayoq kuchga kiradi (kutish yo'q).
        _tenantState.Invalidate(id);
        return await MapWithCountsAsync(tenant);
    }

    public async Task<TenantDto> ActivateAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
        tenant.SubscriptionStatus = SubscriptionStatus.Active;
        tenant.IsActive = true;
        ClearSuspension(tenant);
        await _db.SaveChangesAsync();
        _tenantState.Invalidate(id);
        return await MapWithCountsAsync(tenant);
    }

    /// Qayta yoqishda barcha to'xtatish maydonlari tozalanadi — aks holda keyingi
    /// suspend eski sabab va izoh bilan aralashib ketadi.
    internal static void ClearSuspension(Tenant tenant)
    {
        tenant.SuspendReason = null;
        tenant.SuspendNote = null;
        tenant.SuspendPublicMessage = null;
        tenant.SuspendedUntil = null;
        tenant.SuspendedAt = null;
        tenant.SuspendedByUserId = null;
    }

    public async Task<TenantDto> AssignPlanAsync(int id, int planId)
    {
        var tenant = await _db.Tenants.FindAsync(id) ?? throw new NotFoundException("Tenant not found");
        var plan = await _db.Plans.FindAsync(planId) ?? throw new NotFoundException("Plan not found");

        tenant.PlanId = plan.Id;
        tenant.PlanType = plan.Code;
        ApplyStatusRules(tenant, plan, null);
        await _db.SaveChangesAsync();
        await PlanModules.ApplyPlanModulesAsync(_db, tenant.Id, plan);
        _tenantState.Invalidate(id);

        var userCount = await _db.Users.CountAsync(u => u.TenantId == id);
        return MapToDto(tenant, userCount, plan.Name, await GetInnAsync(tenant.OrganizationId));
    }

    private async Task<TenantDto> MapWithCountsAsync(Tenant tenant)
    {
        var userCount = await _db.Users.CountAsync(u => u.TenantId == tenant.Id);
        var planName = tenant.PlanId != null
            ? (await _db.Plans.FindAsync(tenant.PlanId.Value))?.Name
            : null;
        return MapToDto(tenant, userCount, planName, await GetInnAsync(tenant.OrganizationId));
    }
}
