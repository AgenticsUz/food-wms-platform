using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Application.DTOs.Subscription;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Plan limitlarini majburlaydi (MaxWarehouses / MaxTransfersPerMonth) va ko'rsatadi (+ MaxUsers).
///
/// Qoida: plani bor tenant → plan limitlari; plani yo'q tenant → cheksiz. Limit oshsa 402
/// (PaymentRequiredException) qaytadi.
/// </summary>
/// <remarks>
/// <para>
/// F6 (D4): tenant parametri O'CHDI — tenant <see cref="WmsDbContext.CurrentTenantId"/> dan, hisoblar
/// global filtr + RLS bilan o'z-o'zidan joriy tenantga tushadi. Kontekst yo'q bo'lsa tekshiradigan
/// narsa yo'q (RLS kontekstsiz yozuvni baribir rad etadi).
/// </para>
/// <para>
/// ⚠️ <b>Foydalanuvchi limiti faqat KO'RSATILADI, bloklamaydi.</b> SQLite davrida WMS odamni o'zi
/// yaratardi va 402 «yaratish»ni to'xtatardi. Endi odamni Console Identity'da ochib zavodga
/// biriktiradi (D7) va u birinchi tokenda JIT keladi: shu yerda rad etish Console allaqachon kirgizgan
/// odamni eshik oldida to'xtatib qo'yardi — na u, na tenant admini sababini tushunmasdi, tuzatish
/// esa Console'da. Shuning uchun <c>EnsureCanAddUserAsync</c>/<c>GetRemainingUsersAsync</c> o'chdi;
/// son <c>/api/subscription/me</c> va Console kartasida (<c>usage.users</c>) ko'rinadi.
/// </para>
/// </remarks>
public static class PlanLimits
{
    /// Xom hisoblar — cheklovlar ham, ko'rsatish ham shundan quriladi.
    public record LimitUsageSnapshot(int Users, int Warehouses, int TransfersThisMonth);

    public const string Users = "users";
    public const string Warehouses = "warehouses";
    public const string TransfersThisMonth = "transfersThisMonth";

    private static async Task<Plan?> GetPlanAsync(WmsDbContext db, CancellationToken ct = default)
    {
        if (db.CurrentTenantId is not { } tenantId) return null;

        // `tenant` va `plan` — platforma jadvallari (RLS yo'q), shuning uchun id bo'yicha oshkora.
        return await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId && t.PlanId != null)
            .Select(t => t.Plan)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task EnsureCanAddWarehouseAsync(WmsDbContext db, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(db, ct);
        if (plan is not { MaxWarehouses: > 0 }) return;

        var used = await db.Warehouses.CountAsync(ct);
        if (used >= plan.MaxWarehouses)
            throw new PaymentRequiredException("limit_warehouses",
                Messages.LimitWarehouses, plan.Name, plan.MaxWarehouses);
    }

    public static async Task EnsureCanCreateTransferAsync(WmsDbContext db, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(db, ct);
        if (plan is not { MaxTransfersPerMonth: > 0 }) return;

        var monthStart = MonthStart(DateTime.UtcNow);
        var used = await db.Transfers.CountAsync(t => t.CreatedAt >= monthStart, ct);
        if (used >= plan.MaxTransfersPerMonth)
            throw new PaymentRequiredException("limit_transfers",
                Messages.LimitTransfers, plan.Name, plan.MaxTransfersPerMonth);
    }

    /// <summary>
    /// Yaratishdan KEYIN chaqiriladi: foydalanish ogohlantirish chegarasini (default 80 %)
    /// kesib o'tgan bo'lsa, javobga ogohlantirish qo'shiladi. Bu xato emas — mijoz limitga
    /// urilib qo'ng'iroq qilishidan oldin bilib tursin.
    /// </summary>
    /// <param name="kind"><see cref="Warehouses"/> | <see cref="TransfersThisMonth"/> | <see cref="Users"/>.</param>
    public static async Task ReportUsageAsync(WmsDbContext db, IRequestWarnings warnings,
        string kind, int warnPercent, CancellationToken ct = default)
    {
        // Bu metod yozuv MUVAFFAQIYATLI saqlangandan keyin chaqiriladi. Shuning uchun bu
        // yerdagi hech qanday nosozlik so'rovni yiqitmasligi kerak: aks holda mijoz "xato"
        // deb o'ylab qayta yuboradi va dublikat yozuv paydo bo'ladi. Ogohlantirish —
        // qulaylik, amaliyotning o'zi emas.
        try
        {
            var plan = await GetPlanAsync(db, ct);
            if (plan == null) return;   // plansiz tenant — cheklov ham, ogohlantirish ham yo'q

            // MonthStart'ni so'rovdan TASHQARIDA hisoblaymiz: ifoda ichida qolsa EF uni
            // SQL'ga tarjima qila olmaydi va butun amaliyot qulaydi.
            var monthStart = MonthStart(DateTime.UtcNow);

            var (used, max, code, template) = kind switch
            {
                Users => (await CountUsersAsync(db, ct), plan.MaxUsers,
                    "limit_warn_users", Messages.LimitWarnUsers),
                Warehouses => (await db.Warehouses.CountAsync(ct), plan.MaxWarehouses,
                    "limit_warn_warehouses", Messages.LimitWarnWarehouses),
                _ => (await db.Transfers.CountAsync(t => t.CreatedAt >= monthStart, ct),
                    plan.MaxTransfersPerMonth, "limit_warn_transfers", Messages.LimitWarnTransfers)
            };

            if (max <= 0) return;
            if (Percent(used, max) < warnPercent) return;

            warnings.Add(code, template, used, max);
        }
#pragma warning disable CA1031 // Ogohlantirish yiqilsa saqlangan yozuv baribir muvaffaqiyatli (izoh yuqorida).
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            Console.Error.WriteLine($"[PlanLimits] usage warning for tenant {db.CurrentTenantId} ({kind}) failed: {ex.Message}");
        }
    }

    /// Foizni bitta joyda hisoblaymiz — frontend o'zi hisoblasa, vaqt o'tib ikkovi ajraladi.
    public static decimal Percent(int used, int max)
        => max <= 0 ? 0 : Math.Round(used * 100m / max, 1);

    /// Joriy foydalanish — <c>/api/subscription/me</c> va Console kartasi uchun (kontekst — joriy tenant).
    public static async Task<LimitUsageSnapshot> GetUsageAsync(WmsDbContext db, CancellationToken ct = default)
    {
        var monthStart = MonthStart(DateTime.UtcNow);
        return new LimitUsageSnapshot(
            await CountUsersAsync(db, ct),
            await db.Warehouses.CountAsync(ct),
            await db.Transfers.CountAsync(t => t.CreatedAt >= monthStart, ct));
    }

    /// Limit + foydalanish juftliklari (mijozga ko'rsatiladigan shakl).
    public static async Task<SubscriptionLimitsDto> GetLimitsAsync(WmsDbContext db, Plan? plan,
        int warnPercent, CancellationToken ct = default)
    {
        var usage = await GetUsageAsync(db, ct);

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

    /// <summary>
    /// Faol profillar. O'chirilgan (WMS o'chirgichi) profil hisoblanmaydi: tenant admini ketgan
    /// xodimni o'chirib, o'rniga yangisini olganda limit «to'lib» qolmasin.
    /// </summary>
    private static Task<int> CountUsersAsync(WmsDbContext db, CancellationToken ct)
        => db.UserProfiles.CountAsync(u => u.IsActive, ct);

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
