using System.Text.Json;
using System.Text.Json.Serialization;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Platform;

// Console yuzasining (`/admin/v1`) enumlari SATR bo'lib, camelCase'da yuradi ("bankTransfer",
// "nonPayment") — Wash'ning admin yuzasi bilan bir xil: Console'ning Wash bo'limi shu shaklni
// o'qiydi va WMS bo'limi uning qo'shnisi. Raqam bo'lsa server tomonida enum a'zosi qo'shilganda
// Console jimgina noto'g'ri holatni ko'rsatardi. O'qishda registr ahamiyatsiz.

/// <summary>Enum ↔ camelCase satr (Console yuzasi uchun).</summary>
public sealed class CamelCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public CamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false) { }
}

// ── Qo'lda billing (S1) ─────────────────────────────────────────────────────

/// <summary><c>POST /admin/v1/tenants/{id}/payments</c> tanasi (Wash'dagi <c>periodFrom/periodTo</c> nomlari bilan).</summary>
public class RecordPaymentDto
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    [JsonConverter(typeof(CamelCaseEnumConverter<PlatformPaymentMethod>))]
    public PlatformPaymentMethod Method { get; set; } = PlatformPaymentMethod.BankTransfer;
    public string? Note { get; set; }
}

public class PaymentRecordDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    [JsonConverter(typeof(CamelCaseEnumConverter<PlatformPaymentMethod>))]
    public PlatformPaymentMethod Method { get; set; }
    public string? Note { get; set; }
    /// Qayd etgan Console operatorining Identity <c>sub</c>'i. SQLite davridagi <c>RecordedByName</c>
    /// o'chdi: operatorning WMS profili yo'q, ismi Identity'da (Console uni o'zi yechadi).
    public Guid? RecordedBySub { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// Row of the "who runs out soon" list the platform dashboard shows.
public class ExpiringTenantDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? PlanCode { get; set; }
    public string? PlanName { get; set; }
    [JsonConverter(typeof(CamelCaseEnumConverter<SubscriptionStatus>))]
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? PaidUntil { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    /// Days until whichever date applies (paid period for Active, trial for Trial).
    /// Negative means already past — the tenant is inside its grace period.
    public int DaysLeft { get; set; }
    public string Kind { get; set; } = "paid";   // "paid" | "trial"
}

// ── Suspension (S2) ──────────────────────────────────────────────────────────

public class SuspendTenantDto
{
    [JsonConverter(typeof(CamelCaseEnumConverter<SuspendReason>))]
    public SuspendReason Reason { get; set; } = SuspendReason.Other;
    /// Internal note. Never returned to the tenant.
    public string? Note { get; set; }
    /// Shown to the tenant instead of the generic message.
    public string? PublicMessage { get; set; }
    /// Auto-reactivation date; null = until manually activated.
    public DateTime? Until { get; set; }
}

// F6: lidlar (D9 — demo so'rovi agentics.uz formasi orqali), tashkilotlar (D10 — global INN
// katalogi RLS ostida tenantlar orasida ma'lumot oqizardi) va portal «upgrade-interest» DTO'lari O'CHDI.

// ── Features (S4, S5) ────────────────────────────────────────────────────────

public class FeatureDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ModuleCode { get; set; }
    public bool DefaultEnabled { get; set; }
    public bool IsCustom { get; set; }
    public Guid? OwnerTenantId { get; set; }
    public string? OwnerTenantName { get; set; }
    public string? Reason { get; set; }
    public DateTime? RequestedAt { get; set; }
    public int SortOrder { get; set; }
}

public class CreateFeatureDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ModuleCode { get; set; }
    public bool DefaultEnabled { get; set; } = true;
    public bool IsCustom { get; set; }
    public Guid? OwnerTenantId { get; set; }
    public string? Reason { get; set; }
    public int SortOrder { get; set; }
}

/// A feature as it applies to one tenant, with where the value came from.
public class TenantFeatureDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ModuleCode { get; set; }
    /// Wash'dagi <c>enabled</c> nomi — Console ikkala mahsulotda bitta ekran mantiqini ishlatsin.
    public bool Enabled { get; set; }
    /// "override" (tenantga oshkora) | "plan" | "default" | "module" (egasi modul o'chiq bo'lgani
    /// uchun majburan o'chiq). SQLite davrida override "tenant" edi — Wash nomiga moslandi.
    public string Source { get; set; } = null!;
    public string? Note { get; set; }

    // Custom-feature provenance: the console warns before granting one client's feature
    // to a different client, and shows why it was built.
    public bool IsCustom { get; set; }
    public Guid? OwnerTenantId { get; set; }
    public string? OwnerTenantName { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// <c>PUT /admin/v1/tenants/{id}/features</c> tanasining bitta elementi — tana RO'YXATNING O'ZI
/// (Wash bilan bir xil). <c>Enabled = null</c> override'ni O'CHIRADI (qaror planga qaytadi);
/// ro'yxatda yo'q kodga tegilmaydi.
/// </summary>
public class FeatureOverrideInput
{
    public string Code { get; set; } = null!;
    public bool? Enabled { get; set; }
    public string? Note { get; set; }
}
