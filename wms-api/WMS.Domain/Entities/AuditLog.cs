namespace WMS.Domain.Entities;

/// <summary>
/// O'zgartiruvchi amallar (POST/PUT/DELETE) tarixi — kim, qachon, nimani.
/// Append-only: BaseEntity'dan meros olmaydi (soft-delete/UpdatedAt kerak emas).
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }        // snapshot (user keyin o'chsa ham qoladi)
    public string Action { get; set; } = null!;   // HTTP metod: POST/PUT/DELETE/PATCH
    public string EntityType { get; set; } = null!; // controller nomi (masalan "Transfers")
    public string? EntityAction { get; set; }     // action nomi (masalan "Confirm")
    public int? EntityId { get; set; }            // route'dagi id (bo'lsa)
    public string Path { get; set; } = null!;      // so'rov yo'li
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
