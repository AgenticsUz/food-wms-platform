using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// <c>wms.tenant</c> — zavod (mijoz tashkilot). PLATFORMA jadvali: <c>tenant_id</c>
/// ustuni yo'q, RLS yo'q.
/// </summary>
/// <remarks>
/// <para>⚠️ Jadval IKKI qismdan iborat va ular BOSHQA-BOSHQA egaga tegishli (Wash naqshi):</para>
/// <list type="number">
///   <item><b>Identity NUSXASI</b> — <see cref="BaseEntity.Id"/>, <see cref="Code"/>,
///   <see cref="IdentityStatus"/>, <see cref="Modules"/>, <see cref="SyncedAt"/>.
///   WMS ularni YARATMAYDI: reyestr Identity'da (P4), qator birinchi tokenda JIT
///   yoziladi (<c>WmsPlatformUserSink</c>).</item>
///   <item><b>WMS'ning O'Z tijorat ma'lumoti</b> — plan, trial, to'lov, suspend,
///   brendlash. Uni Console'ning WMS bo'limi <c>/admin/v1/*</c> orqali tahrirlaydi.</item>
/// </list>
/// <para>
/// ⚠️ <see cref="Name"/> tokendan kelmaydi (tokendagi <c>name</c> — ODAMNING ismi).
/// Birinchi yozuvda kod bilan to'ldiriladi, haqiqiy nomni Console qo'yadi
/// (HOLAT §4 #9 — HRM va Wash'da ham ayni hol).
/// </para>
/// </remarks>
public class Tenant : BaseEntity
{
    /// <summary>Identity tomonidagi «faol» holat.</summary>
    public const string IdentityActiveStatus = "active";

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>
    /// Identity'dagi holat. ⚠️ Amalda DOIM <c>active</c>: Identity token'ni faqat
    /// faol tenantga beradi, ya'ni to'xtatish nusxaga yetib bormaydi (HOLAT §4 #10,
    /// javobi — webhook, 2-bosqich). Xavfsizlik uchun yetarli: eshik Identity'da yopiladi.
    /// </summary>
    public string IdentityStatus { get; set; } = IdentityActiveStatus;

    /// <summary>
    /// Yoqilgan modullar — tokendagi <c>modules</c> claim'i, BO'SH JOY bilan
    /// ajratilgan (tokendagi shaklda). Modul yoqish FAQAT Identity'da (D6):
    /// SQLite davridagi <c>TenantModule</c> jadvali va <c>Plan.ModuleCodes</c> o'chdi,
    /// aks holda «modul yoqiqmi?» savoliga ikki javob chiqardi.
    /// </summary>
    public string Modules { get; set; } = string.Empty;

    public DateTime? SyncedAt { get; set; }

    /// <summary>WMS tomonidagi o'chirgich (Console WMS bo'limi) — Identity holatidan mustaqil.</summary>
    public bool IsActive { get; set; } = true;

    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trial;
    public Guid? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>Qo'lda to'lov: shu sanagacha to'langan. Null — to'lov uchun hech qachon bloklanmaydi.</summary>
    public DateTime? PaidUntil { get; set; }

    public SuspendReason? SuspendReason { get; set; }
    /// <summary>Ichki izoh — tenantga hech qachon qaytarilmaydi.</summary>
    public string? SuspendNote { get; set; }
    /// <summary>Tenantga ko'rsatiladigan matn (umumiy matn o'rniga).</summary>
    public string? SuspendPublicMessage { get; set; }
    /// <summary>Avtomatik qayta yoqilish sanasi. Null — qo'lda yoqilguncha.</summary>
    public DateTime? SuspendedUntil { get; set; }
    public DateTime? SuspendedAt { get; set; }

    /// <summary>To'xtatgan Console operatorining Identity <c>sub</c>'i (uning WMS profili yo'q).</summary>
    public Guid? SuspendedBySub { get; set; }

    /// <summary>Keng logo — yoyilgan sidebar, hisobot sarlavhalari.</summary>
    public string? LogoUrl { get; set; }
    /// <summary>Kvadrat logo — yig'ilgan sidebar, favicon.</summary>
    public string? LogoSquareUrl { get; set; }
    /// <summary>Asosiy rang (#RRGGBB); palitrani mijoz o'zi yasaydi.</summary>
    public string? BrandColor { get; set; }

    /// <summary>
    /// Mijozlarga (kontragent) Telegram xabarlari (TG13): buyurtma tasdiqlandi, yetkazildi, to'lov, qarz
    /// eslatmasi. Sukut O'CHIQ — mijozning mijoziga xabar yuborish tenant qarori.
    /// </summary>
    public bool ClientTelegramEnabled { get; set; }

    /// <summary>Qarz eslatmasi necha kunda bir (0 — o'chiq). Faqat <see cref="ClientTelegramEnabled"/> bilan.</summary>
    public int DebtReminderDays { get; set; }

    /// <summary>Modul kodlari ro'yxat ko'rinishida.</summary>
    public IReadOnlyList<string> ModuleCodes =>
        Modules.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Birinchi tokendan nusxa yaratadi (JIT).</summary>
    public static Tenant FromToken(Guid id, string code, IEnumerable<string>? modules, DateTime utcNow) => new()
    {
        Id = id,
        Code = code,
        Name = code,
        Modules = JoinModules(modules),
        SyncedAt = utcNow,
        CreatedAt = utcNow,
        UpdatedAt = utcNow,
    };

    /// <summary>
    /// Nusxani tokendan yangilaydi. <see langword="true"/> — biror ustun haqiqatan o'zgardi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bo'sh modul ro'yxati «modul yo'q» degani EMAS: claim tokenda umuman
    /// bo'lmasligi mumkin. Uni o'zgarish deb qabul qilsak nusxadagi haqiqiy ro'yxat
    /// o'chib, <c>[RequireModule]</c> hamma ekranni yopardi (Wash F4 darsi).
    /// </remarks>
    public bool SyncFromToken(string? code, IEnumerable<string>? modules, DateTime utcNow)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(code) && !string.Equals(Code, code, StringComparison.Ordinal))
        {
            Code = code;
            changed = true;
        }

        string joined = JoinModules(modules);
        if (joined.Length > 0 && !string.Equals(Modules, joined, StringComparison.Ordinal))
        {
            Modules = joined;
            changed = true;
        }

        if (!string.Equals(IdentityStatus, IdentityActiveStatus, StringComparison.Ordinal))
        {
            IdentityStatus = IdentityActiveStatus;
            changed = true;
        }

        if (changed)
        {
            SyncedAt = utcNow;
        }

        return changed;
    }

    private static string JoinModules(IEnumerable<string>? modules) =>
        modules is null
            ? string.Empty
            : string.Join(' ', modules
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.Ordinal));
}
