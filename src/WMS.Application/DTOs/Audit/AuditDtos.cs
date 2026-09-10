namespace WMS.Application.DTOs.Audit;

/// <summary>Tenantning o'z jurnali qatori (<c>GET /api/audit</c>).</summary>
/// <remarks>
/// F6: platforma ko'rinishining maydonlari (<c>TenantId</c>, <c>TenantName</c>,
/// <c>ActorTenantId</c>, <c>ActorTenantName</c>) va <c>AdminAuditQuery</c> O'CHDI — barcha
/// tenantlar jurnali endi yo'q (RLS), Console <c>/admin/v1/audit</c> dan o'qiydi.
/// <c>UserId</c> (<c>int</c>) o'rniga <see cref="ActorSub"/>: jurnal Console operatorini ham
/// yozadi, uning esa bu tenantda profili yo'q.
/// </remarks>
public class AuditLogDto
{
    public Guid Id { get; set; }

    /// <summary>Bajaruvchining Identity <c>sub</c>'i.</summary>
    public Guid? ActorSub { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityAction { get; set; }

    /// <summary>Marshrutdagi <c>id</c> — satr (Guid kalit yoki boshqa marshrut qiymati).</summary>
    public string? EntityId { get; set; }
    public string Path { get; set; } = null!;
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
    /// Platforma (Console operatori) amali — masalan tenantni suspend qilish.
    /// Mijoz o'z izida kim nima qilganini ko'rishi uchun ajratiladi.
    public bool IsPlatformAction { get; set; }
}
