using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// Sotiladigan imkoniyat — moduldan bir daraja nozikroq (<c>production.recipes</c>).
/// Platforma katalogi — <c>tenant_id</c> yo'q.
/// </summary>
/// <remarks>
/// Qatlamlar (yirikdan noziklikka): Identity moduli (nima sotildi) → plan/override
/// feature'i (tenantda nima bor) → ruxsat (tenant ichida kim ishlata oladi).
/// </remarks>
public class Feature : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Egasi modul. Modul o'chiq bo'lsa feature ham o'chiq. Null — kesishuvchi (eksport, import).</summary>
    public string? ModuleCode { get; set; }

    public bool DefaultEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    // ── Custom feature'lar (bitta mijoz uchun yozilgan, docs/CUSTOM_FEATURES.md) ──
    public bool IsCustom { get; set; }
    public Guid? OwnerTenantId { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Feature'ning tenant uchun override'i. Bor bo'lsa — hal qiluvchi (plandan ustun).
/// </summary>
public class TenantFeature : TenantEntity
{
    public Tenant Tenant { get; set; } = null!;
    public string FeatureCode { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string? Note { get; set; }

    /// <summary>O'rnatgan Console operatorining Identity <c>sub</c>'i.</summary>
    public Guid? SetBySub { get; set; }
    public DateTime SetAt { get; set; } = DateTime.UtcNow;
}
