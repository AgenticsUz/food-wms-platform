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
}
