namespace WMS.Application.DTOs.Audit;

public class AuditLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityAction { get; set; }
    public int? EntityId { get; set; }
    public string Path { get; set; } = null!;
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
    /// Platforma (SuperAdmin) amali — masalan tenantni suspend qilish.
    /// Mijoz o'z izida kim nima qilganini ko'rishi uchun ajratiladi.
    public bool IsPlatformAction { get; set; }

    /// <summary>
    /// Faqat platforma ko'rinishida (`/api/admin/audit`) to'ldiriladi — tenant ichidagi
    /// jurnalda hamma yozuv bir tenantniki, nom takrorlanishi ma'nosiz.
    /// Frontend har qator uchun alohida so'rov qilmasin deb shu yerda beriladi.
    /// </summary>
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    /// Amalni bajargan tenant (platforma amallarida — platforma tenanti).
    public int? ActorTenantId { get; set; }
    public string? ActorTenantName { get; set; }
}

/// <summary>
/// `/api/admin/audit` filtrlari. Barchasi ixtiyoriy: `tenantId` berilmasa **barcha**
/// tenantlar bo'yicha qaytadi.
/// </summary>
public class AdminAuditQuery
{
    public int? TenantId { get; set; }
    public int? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    /// `true` — faqat `IsPlatformAction` yozuvlari.
    public bool? PlatformOnly { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public const int DefaultPageSize = 50;
    /// Audit — eng tez o'sadigan jadval. Chegara xato emas, jimgina qisqartiriladi.
    public const int MaxPageSize = 200;

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize =>
        PageSize < 1 ? DefaultPageSize : PageSize > MaxPageSize ? MaxPageSize : PageSize;
}
