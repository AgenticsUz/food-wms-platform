using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.DTOs.Branding;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using TenantState = WMS.Application.Common.TenantState;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
public class TenantService : ITenantService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    private readonly IFeatureService _features;
    private readonly ICurrentTenant _currentTenant;
    private readonly SubscriptionOptions _subscription;

    public TenantService(WmsDbContext db, ITenantStateService tenantState, IFeatureService features,
        ICurrentTenant currentTenant, IOptions<SubscriptionOptions> subscription)
    {
        _db = db;
        _tenantState = tenantState;
        _features = features;
        _currentTenant = currentTenant;
        _subscription = subscription.Value;
    }

    public async Task<(List<TenantListItemDto> Items, long Total)> ListAsync(int page, int size, string? search,
        string? status, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);

        // `tenant` — platforma jadvali: ro'yxat tenant kontekstisiz, hamma tenant bo'yicha. Foydalanuvchi
        // soni ATAYLAB yo'q — `user_profile` RLS ostida, uni sanash har tenantga alohida kirish bo'lardi.
        IQueryable<Tenant> query = _db.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Code, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => !t.IsActive);
            else if (Enum.TryParse(status, ignoreCase: true, out SubscriptionStatus parsed) && Enum.IsDefined(parsed))
                query = query.Where(t => t.SubscriptionStatus == parsed);
        }

        var total = await query.LongCountAsync(ct);
        var rows = await query
            .OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new
            {
                t.Id, t.Code, t.Name, t.IsActive, t.PlanId,
                PlanCode = t.Plan != null ? t.Plan.Code : null,
                PlanName = t.Plan != null ? t.Plan.Name : null,
                t.SubscriptionStatus, t.TrialEndsAt, t.PaidUntil, t.SuspendReason, t.SuspendedUntil,
                t.Modules, t.CreatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(t => new TenantListItemDto
        {
            Id = t.Id, Code = t.Code, Name = t.Name, IsActive = t.IsActive,
            PlanId = t.PlanId, PlanCode = t.PlanCode, PlanName = t.PlanName,
            SubscriptionStatus = t.SubscriptionStatus, TrialEndsAt = t.TrialEndsAt, PaidUntil = t.PaidUntil,
            SuspendedReason = t.SuspendReason, SuspendedUntil = t.SuspendedUntil,
            ModuleCount = t.Modules.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = t.CreatedAt
        }).ToList();

        return (items, total);
    }

    public Task<TenantDetailDto> GetAsync(Guid id, CancellationToken ct = default) => LoadDetailAsync(id, ct);

    public async Task<TenantDetailDto> UpdateAsync(Guid id, UpdateTenantDto dto, CancellationToken ct = default)
    {
        var tenant = await FindTrackedAsync(id, ct);

        var name = (dto.Name ?? "").Trim();
        if (name.Length == 0) throw new AppException("Tenant name is required");
        if (name.Length > 200) throw new AppException("Tenant name must be at most 200 characters");

        tenant.Name = name;
        // Faqat kelgan bo'lsa yoziladi — aks holda formada bu maydon bo'lmasa mavjud
        // trial sanasi jimgina o'chib ketardi.
        if (dto.TrialEndsAt.HasValue) tenant.TrialEndsAt = dto.TrialEndsAt;
        if (dto.PaidUntil.HasValue) tenant.PaidUntil = dto.PaidUntil;
        if (dto.IsActive.HasValue) tenant.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync(ct);
        _tenantState.Invalidate(id);
        return await LoadDetailAsync(id, ct);
    }

    public async Task<TenantDetailDto> AssignPlanAsync(Guid id, Guid? planId, CancellationToken ct = default)
    {
        var tenant = await FindTrackedAsync(id, ct);

        Plan? plan = null;
        if (planId is { } value)
        {
            plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == value, ct)
                ?? throw new NotFoundException("Plan not found");
        }

        // Plan olib tashlansa tenant limitsiz va katalog sukutlari bilan ishlaydi (FeatureSource.Default) —
        // o'chib qolmaydi. Modul to'plamiga tegilmaydi: u Identity'da (D6).
        tenant.PlanId = plan?.Id;
        ApplyStatusRules(tenant, plan);
        await _db.SaveChangesAsync(ct);
        _tenantState.Invalidate(id);

        return await LoadDetailAsync(id, ct);
    }

    /// <summary>
    /// Holat mashinasi (avval aniqlanmagan edi — plan biriktirilsa ham status Trial bo'lib
    /// qolishi mumkin edi):
    /// • Pullik plan (Price &gt; 0) biriktirilsa → Active, trial sanasi tozalanadi.
    /// • Trial plan (TrialDays &gt; 0) biriktirilsa va sana yo'q bo'lsa → Trial + sana qo'yiladi.
    /// </summary>
    private static void ApplyStatusRules(Tenant tenant, Plan? plan)
    {
        if (plan == null) return;

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

    public async Task<TenantDetailDto> SuspendAsync(Guid id, SuspendTenantDto? dto, Guid? suspendedBySub,
        CancellationToken ct = default)
    {
        var tenant = await FindTrackedAsync(id, ct);

        tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
        tenant.SuspendReason = dto?.Reason ?? SuspendReason.Other;
        tenant.SuspendNote = string.IsNullOrWhiteSpace(dto?.Note) ? null : dto!.Note!.Trim();
        tenant.SuspendPublicMessage = string.IsNullOrWhiteSpace(dto?.PublicMessage) ? null : dto!.PublicMessage!.Trim();
        tenant.SuspendedUntil = dto?.Until;
        tenant.SuspendedAt = DateTime.UtcNow;
        tenant.SuspendedBySub = suspendedBySub;

        await _db.SaveChangesAsync(ct);
        // Cache tozalanadi — suspend keyingi so'rovdayoq kuchga kiradi (kutish yo'q).
        _tenantState.Invalidate(id);
        return await LoadDetailAsync(id, ct);
    }

    public async Task<TenantDetailDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await FindTrackedAsync(id, ct);
        tenant.SubscriptionStatus = SubscriptionStatus.Active;
        tenant.IsActive = true;
        ClearSuspension(tenant);
        await _db.SaveChangesAsync(ct);
        _tenantState.Invalidate(id);
        return await LoadDetailAsync(id, ct);
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
        tenant.SuspendedBySub = null;
    }

    // ── Platforma ko'rsatkichlari ───────────────────────────────────────────

    public async Task<PlatformStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var growthStart = monthStart.AddMonths(-11);

        // Hisoblar SQL'da (SQLite davrida butun jadval xotiraga olinardi).
        var counts = await _db.Tenants.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Trial = g.Count(t => t.SubscriptionStatus == SubscriptionStatus.Trial),
                Active = g.Count(t => t.SubscriptionStatus == SubscriptionStatus.Active),
                Suspended = g.Count(t => t.SubscriptionStatus == SubscriptionStatus.Suspended),
                Inactive = g.Count(t => !t.IsActive),
                Expiring = g.Count(t => t.IsActive &&
                    ((t.SubscriptionStatus == SubscriptionStatus.Trial && t.TrialEndsAt != null && t.TrialEndsAt <= horizon)
                     || (t.SubscriptionStatus == SubscriptionStatus.Active && t.PlanId != null && t.PaidUntil != null && t.PaidUntil <= horizon))),
                NewThisMonth = g.Count(t => t.CreatedAt >= monthStart),
            })
            .FirstOrDefaultAsync(ct);

        var growthRows = await _db.Tenants.AsNoTracking()
            .Where(t => t.CreatedAt >= growthStart)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        // 12 oy, eskidan yangiga; bo'sh oy ham qatorda (grafikda teshik bo'lmasin).
        var growth = new List<MonthCountDto>(12);
        for (var i = 0; i < 12; i++)
        {
            var month = growthStart.AddMonths(i);
            growth.Add(new MonthCountDto
            {
                Month = month.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                Count = growthRows.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month)?.Count ?? 0
            });
        }

        return new PlatformStatsDto
        {
            Total = counts?.Total ?? 0,
            Trial = counts?.Trial ?? 0,
            Active = counts?.Active ?? 0,
            Suspended = counts?.Suspended ?? 0,
            Inactive = counts?.Inactive ?? 0,
            Expiring7d = counts?.Expiring ?? 0,
            NewThisMonth = counts?.NewThisMonth ?? 0,
            MonthlyGrowth = growth
        };
    }

    // ── Karta ───────────────────────────────────────────────────────────────

    private async Task<Tenant> FindTrackedAsync(Guid id, CancellationToken ct)
    {
        RequireContext(id);
        return await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Tenant not found");
    }

    /// <summary>
    /// Karta — Console'ning bitta ekrani: tijorat qatlami, feature'lar (manbasi bilan), limit va
    /// foydalanish, brendlash va wms-web ko'radigan kirish hukmi.
    /// </summary>
    private async Task<TenantDetailDto> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        RequireContext(id);

        var tenant = await _db.Tenants.AsNoTracking().Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("Tenant not found");

        // Kesh bekor: operator hozirgina nimanidir o'zgartirgan bo'lishi mumkin va karta
        // «eski» hukmni ko'rsatsa, u o'z amalining natijasiga ishonmay qolardi.
        _tenantState.Invalidate(id);
        TenantState? state = await _tenantState.GetAsync(id, ct);
        var verdict = SubscriptionPolicy.Evaluate(state, _subscription, DateTime.UtcNow);

        var features = await _features.GetTenantFeaturesAsync(id, ct);
        var usage = await PlanLimits.GetUsageAsync(_db, ct);
        var plan = tenant.Plan;

        return new TenantDetailDto
        {
            Id = tenant.Id, Code = tenant.Code, Name = tenant.Name,
            IsActive = tenant.IsActive,
            IdentityStatus = tenant.IdentityStatus,
            Modules = [.. tenant.ModuleCodes],
            SyncedAt = tenant.SyncedAt,
            PlanId = tenant.PlanId, PlanCode = plan?.Code, PlanName = plan?.Name,
            SubscriptionStatus = tenant.SubscriptionStatus,
            TrialEndsAt = tenant.TrialEndsAt, PaidUntil = tenant.PaidUntil,
            SuspendedReason = tenant.SuspendReason, SuspendedNote = tenant.SuspendNote,
            SuspendedPublicMessage = tenant.SuspendPublicMessage, SuspendedUntil = tenant.SuspendedUntil,
            SuspendedAt = tenant.SuspendedAt, SuspendedBySub = tenant.SuspendedBySub,
            Access = new TenantAccessDto { Allowed = verdict.Allowed, Code = verdict.Code },
            Branding = new BrandingDto { LogoUrl = tenant.LogoUrl, LogoSquareUrl = tenant.LogoSquareUrl, BrandColor = tenant.BrandColor },
            Features = features,
            Limits = new TenantLimitsDto
            {
                MaxUsers = Limit(plan?.MaxUsers),
                MaxWarehouses = Limit(plan?.MaxWarehouses),
                MaxTransfersPerMonth = Limit(plan?.MaxTransfersPerMonth)
            },
            Usage = new TenantUsageDto { Users = usage.Users, Warehouses = usage.Warehouses, TransfersThisMonth = usage.TransfersThisMonth },
            CreatedAt = tenant.CreatedAt
        };
    }

    /// 0 yoki plan yo'q — cheksiz (<c>null</c>), Console «0 dan 0» ko'rsatib chalg'itmasin.
    private static int? Limit(int? value) => value is > 0 ? value : null;

    /// <summary>
    /// Karta RLS jadvallarini ham o'qiydi: kontekst boshqa tenantda yoki bo'sh bo'lsa hisoblar jimgina
    /// 0 bo'lib, «ombori yo'q, xodimi yo'q» degan YOLG'ON karta chiqardi. Dasturchi xatosi — darhol yiqilsin.
    /// </summary>
    private void RequireContext(Guid id)
    {
        if (_currentTenant.TenantId != id)
            throw new InvalidOperationException(
                $"Tenant {id} kartasi uchun tenant konteksti o'rnatilmagan (AdminBaseController.UseTenant chaqirilmagan).");
    }
}
