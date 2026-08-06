using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
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

    /// <summary>
    /// Yaratishdan KEYIN chaqiriladi: foydalanish ogohlantirish chegarasini (default 80 %)
    /// kesib o'tgan bo'lsa, javobga ogohlantirish qo'shiladi. Bu xato emas — mijoz limitga
    /// urilib qo'ng'iroq qilishidan oldin bilib tursin.
    /// </summary>
    public static async Task ReportUsageAsync(WmsDbContext db, IRequestWarnings warnings, int tenantId,
        string kind, int warnPercent, CancellationToken ct = default)
    {
        // Bu metod yozuv MUVAFFAQIYATLI saqlangandan keyin chaqiriladi. Shuning uchun bu
        // yerdagi hech qanday nosozlik so'rovni yiqitmasligi kerak: aks holda mijoz "xato"
        // deb o'ylab qayta yuboradi va dublikat yozuv paydo bo'ladi. Ogohlantirish —
        // qulaylik, amaliyotning o'zi emas.
        try
        {
            var plan = await GetPlanAsync(db, tenantId, ct);
            if (plan == null) return;   // plansiz tenant — cheklov ham, ogohlantirish ham yo'q

            // MonthStart'ni so'rovdan TASHQARIDA hisoblaymiz: ifoda ichida qolsa EF uni
            // SQL'ga tarjima qila olmaydi va butun amaliyot qulaydi.
            var monthStart = MonthStart(DateTime.UtcNow);

            var (used, max, code, template) = kind switch
            {
                Users => (await db.Users.CountAsync(u => u.TenantId == tenantId, ct), plan.MaxUsers,
                    "limit_warn_users", Messages.LimitWarnUsers),
                Warehouses => (await db.Warehouses.CountAsync(w => w.TenantId == tenantId, ct), plan.MaxWarehouses,
                    "limit_warn_warehouses", Messages.LimitWarnWarehouses),
                _ => (await db.Transfers.CountAsync(t => t.TenantId == tenantId && t.CreatedAt >= monthStart, ct),
                    plan.MaxTransfersPerMonth, "limit_warn_transfers", Messages.LimitWarnTransfers)
            };

            if (max <= 0) return;
            if (Percent(used, max) < warnPercent) return;

            warnings.Add(code, template, used, max);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PlanLimits] usage warning for tenant {tenantId} ({kind}) failed: {ex.Message}");
        }
    }

    /// Foizni bitta joyda hisoblaymiz — frontend o'zi hisoblasa, vaqt o'tib ikkovi ajraladi.
    public static decimal Percent(int used, int max)
        => max <= 0 ? 0 : Math.Round(used * 100m / max, 1);

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
        int warnPercent, CancellationToken ct = default)
    {
        var usage = await GetUsageAsync(db, tenantId, plan, ct);

        return new SubscriptionLimitsDto
        {
            MaxUsers = plan?.MaxUsers ?? 0,
            CurrentUsers = usage.Users,
            MaxWarehouses = plan?.MaxWarehouses ?? 0,
            CurrentWarehouses = usage.Warehouses,
            MaxTransfersPerMonth = plan?.MaxTransfersPerMonth ?? 0,
            CurrentTransfersThisMonth = usage.TransfersThisMonth,

            Users = Detail(usage.Users, plan?.MaxUsers ?? 0, warnPercent),
            Warehouses = Detail(usage.Warehouses, plan?.MaxWarehouses ?? 0, warnPercent),
            Transfers = Detail(usage.TransfersThisMonth, plan?.MaxTransfersPerMonth ?? 0, warnPercent)
        };
    }

    /// Limitsiz (plansiz) tenantda foiz ham, ogohlantirish ham ma'nosiz → null.
    private static LimitUsageDto Detail(int used, int max, int warnPercent)
    {
        if (max <= 0) return new LimitUsageDto { Max = 0, Current = used };

        var percent = Percent(used, max);
        return new LimitUsageDto
        {
            Max = max, Current = used,
            UsagePercent = percent,
            IsNearLimit = percent >= warnPercent
        };
    }

    private static DateTime MonthStart(DateTime utcNow)
        => new(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
}
