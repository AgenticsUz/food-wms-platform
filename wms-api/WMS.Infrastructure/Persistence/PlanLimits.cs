using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Subscription;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Plan limitlarini majburlaydi (MaxUsers / MaxWarehouses / MaxTransfersPerMonth).
/// Avval bu qiymatlar faqat saqlanardi — hech qayerda tekshirilmasdi.
///
/// Qoida: plani bor tenant → plan limitlari; plani yo'q tenant (tizim tenanti va eski
/// yozuvlar) → cheksiz. Limit oshsa 402 (PaymentRequiredException) qaytadi.
/// </summary>
public static class PlanLimits
{
    /// Xom hisoblar — cheklovlar ham, ko'rsatish ham shundan quriladi.
    public record LimitUsageSnapshot(int Users, int Warehouses, int TransfersThisMonth);

    public const string Users = "users";
    public const string Warehouses = "warehouses";
    public const string TransfersThisMonth = "transfersThisMonth";

    private static async Task<Plan?> GetPlanAsync(WmsDbContext db, int tenantId, CancellationToken ct = default)
    {
        var planId = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.PlanId)
            .FirstOrDefaultAsync(ct);
        return planId == null ? null : await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct);
    }

    public static async Task EnsureCanAddUserAsync(WmsDbContext db, int tenantId, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(db, tenantId, ct);
        if (plan is not { MaxUsers: > 0 }) return;

        var used = await db.Users.CountAsync(u => u.TenantId == tenantId, ct);
        if (used >= plan.MaxUsers)
            throw new PaymentRequiredException("limit_users",
                Messages.LimitUsers, plan.Name, plan.MaxUsers);
    }

    /// Nechta foydalanuvchi qo'shish mumkin. <c>null</c> = cheksiz (plansiz tenant).
    /// Ommaviy import bitta-bitta 402 tashlash o'rniga shu bilan hisoblab boradi.
    public static async Task<int?> GetRemainingUsersAsync(WmsDbContext db, int tenantId, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(db, tenantId, ct);
        if (plan is not { MaxUsers: > 0 }) return null;

        var used = await db.Users.CountAsync(u => u.TenantId == tenantId, ct);
        return Math.Max(0, plan.MaxUsers - used);
    }

    public static async Task EnsureCanAddWarehouseAsync(WmsDbContext db, int tenantId, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(db, tenantId, ct);
        if (plan is not { MaxWarehouses: > 0 }) return;

        var used = await db.Warehouses.CountAsync(w => w.TenantId == tenantId, ct);
        if (used >= plan.MaxWarehouses)
            throw new PaymentRequiredException("limit_warehouses",
                Messages.LimitWarehouses, plan.Name, plan.MaxWarehouses);
    }

    public static async Task EnsureCanCreateTransferAsync(WmsDbContext db, int tenantId, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(db, tenantId, ct);
        if (plan is not { MaxTransfersPerMonth: > 0 }) return;

        var monthStart = MonthStart(DateTime.UtcNow);
        var used = await db.Transfers.CountAsync(t => t.TenantId == tenantId && t.CreatedAt >= monthStart, ct);
        if (used >= plan.MaxTransfersPerMonth)
            throw new PaymentRequiredException("limit_transfers",
                Messages.LimitTransfers, plan.Name, plan.MaxTransfersPerMonth);
    }

    /// Joriy foydalanish — /api/subscription/me uchun.
    public static async Task<LimitUsageSnapshot> GetUsageAsync(WmsDbContext db, int tenantId, Plan? plan,
        CancellationToken ct = default)
    {
        var monthStart = MonthStart(DateTime.UtcNow);
        return new LimitUsageSnapshot(
            await db.Users.CountAsync(u => u.TenantId == tenantId, ct),
            await db.Warehouses.CountAsync(w => w.TenantId == tenantId, ct),
            await db.Transfers.CountAsync(t => t.TenantId == tenantId && t.CreatedAt >= monthStart, ct));
    }

    /// Limit + foydalanish juftliklari (mijozga ko'rsatiladigan shakl).
    public static async Task<SubscriptionLimitsDto> GetLimitsAsync(WmsDbContext db, int tenantId, Plan? plan,
        CancellationToken ct = default)
    {
        var usage = await GetUsageAsync(db, tenantId, plan, ct);
        return new SubscriptionLimitsDto
        {
            MaxUsers = plan?.MaxUsers ?? 0,
            CurrentUsers = usage.Users,
            MaxWarehouses = plan?.MaxWarehouses ?? 0,
            CurrentWarehouses = usage.Warehouses,
            MaxTransfersPerMonth = plan?.MaxTransfersPerMonth ?? 0,
            CurrentTransfersThisMonth = usage.TransfersThisMonth
        };
    }

    private static DateTime MonthStart(DateTime utcNow)
        => new(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
}
