using System.Text.Json.Serialization;
using WMS.Application.DTOs.Branding;
using WMS.Application.DTOs.Platform;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Tenants;

// Console'ning WMS bo'limi (`/admin/v1/tenants*`) uchun. F6 da O'CHDI: `CreateTenantDto` (tenant
// Identity'da ochiladi, WMS nusxani JIT oladi — P4), `Slug` (login Identity'da), `Inn`/`OrganizationId`
// (D10), modul DTO'lari (`TenantModuleDto`, `ToggleModule*` — D6: modul faqat Identity'da).

/// <summary>Tenantlar ro'yxatining qatori (<c>GET /admin/v1/tenants</c>).</summary>
public class TenantListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanCode { get; set; }
    public string? PlanName { get; set; }
    [JsonConverter(typeof(CamelCaseEnumConverter<SubscriptionStatus>))]
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? PaidUntil { get; set; }
    [JsonConverter(typeof(CamelCaseEnumConverter<SuspendReason>))]
    public SuspendReason? SuspendedReason { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    /// Identity obunasidagi modullar soni (nusxadan).
    public int ModuleCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Tenant kartasi (<c>GET /admin/v1/tenants/{id}</c>) — Console'ning bitta ekrani uchun hammasi.</summary>
public class TenantDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// WMS o'chirgichi (Identity holatidan mustaqil).
    public bool IsActive { get; set; }
    /// Identity'dagi holat va modullar — FAQAT o'qish, manba Identity (D6).
    public string IdentityStatus { get; set; } = null!;
    public List<string> Modules { get; set; } = new();
    public DateTime? SyncedAt { get; set; }

    public Guid? PlanId { get; set; }
    public string? PlanCode { get; set; }
    public string? PlanName { get; set; }
    [JsonConverter(typeof(CamelCaseEnumConverter<SubscriptionStatus>))]
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? PaidUntil { get; set; }

    // SuspendedNote ICHKI izoh — bu DTO faqat Console'ga ketadi, tenant javobiga hech qachon tushmaydi.
    [JsonConverter(typeof(CamelCaseEnumConverter<SuspendReason>))]
    public SuspendReason? SuspendedReason { get; set; }
    public string? SuspendedNote { get; set; }
    public string? SuspendedPublicMessage { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public Guid? SuspendedBySub { get; set; }

    /// Hozir ishlay oladimi — wms-web ko'radigan hukmning o'zi (<c>SubscriptionPolicy</c>). Qo'ng'iroq
    /// qilgan mijozga «nega bloklangan» ni operator taxmin qilmasin.
    public TenantAccessDto Access { get; set; } = new();

    public BrandingDto Branding { get; set; } = new();
    public List<TenantFeatureDto> Features { get; set; } = new();
    public TenantLimitsDto Limits { get; set; } = new();
    public TenantUsageDto Usage { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class TenantAccessDto
{
    public bool Allowed { get; set; }
    /// <c>SubscriptionPolicy</c> kodi (<c>trial_expired</c>, <c>suspended_nonpayment</c>, ...) yoki null.
    public string? Code { get; set; }
}

/// <summary>Plan limitlari; <c>null</c> — cheksiz (plan yo'q yoki limit 0).</summary>
public class TenantLimitsDto
{
    public int? MaxUsers { get; set; }
    public int? MaxWarehouses { get; set; }
    public int? MaxTransfersPerMonth { get; set; }
}

public class TenantUsageDto
{
    /// Faol WMS profillari (JIT yozgan). Limit faqat ko'rsatiladi — bloklamaydi (<c>PlanLimits</c> izohi).
    public int Users { get; set; }
    public int Warehouses { get; set; }
    public int TransfersThisMonth { get; set; }
}

/// <summary>
/// <c>PUT /admin/v1/tenants/{id}</c>. <c>null</c> maydon — o'zgarmaydi (SQLite davridagi qoida:
/// formada bo'lmagan maydon mavjud sanani jimgina o'chirib yubormasin).
/// </summary>
/// <remarks>
/// <see cref="Name"/> — zavodning ko'rsatiladigan nomi: tokenda yo'q (u yerdagi <c>name</c> —
/// ODAMNING ismi), shuning uchun nusxaga Console qo'yadi (HOLAT §4 #9).
/// </remarks>
public class UpdateTenantDto
{
    public string Name { get; set; } = null!;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? PaidUntil { get; set; }
    public bool? IsActive { get; set; }
}
