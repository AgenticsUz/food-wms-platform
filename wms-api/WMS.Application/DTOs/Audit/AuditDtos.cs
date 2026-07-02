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
}
