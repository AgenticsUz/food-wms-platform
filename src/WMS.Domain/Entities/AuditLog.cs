using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// O'zgartiruvchi amallar tarixi — kim, qachon, nimani. Append-only:
/// <see cref="BaseEntity"/> dan meros olmaydi (soft delete ham, UpdatedAt ham yo'q)
/// va ilova roliga UPDATE/DELETE huquqi berilmaydi (<c>WmsDatabaseMigrator</c>).
/// </summary>
/// <remarks>
/// Tenant RLS ostida. SQLite davridagi «barcha tenantlar jurnali» (platforma
/// sahifasi) endi yo'q: Console tenantni oshkora tanlaydi (§4.4 — «platforma
/// admini RLS'ni chetlab o'tmaydi»).
/// </remarks>
public class AuditLog : ITenantEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }

    /// <summary>Bajaruvchining Identity <c>sub</c>'i (Console operatori ham bo'lishi mumkin).</summary>
    public Guid? ActorSub { get; set; }

    /// <summary>Ism nusxasi — foydalanuvchi keyin o'zgarsa ham qoladi.</summary>
    public string? UserName { get; set; }

    /// <summary>HTTP metod: POST/PUT/DELETE/PATCH.</summary>
    public string Action { get; set; } = null!;

    /// <summary>Controller nomi (<c>Transfers</c>).</summary>
    public string EntityType { get; set; } = null!;

    /// <summary>Action nomi (<c>Confirm</c>).</summary>
    public string? EntityAction { get; set; }

    /// <summary>Marshrutdagi <c>id</c> (bo'lsa).</summary>
    public string? EntityId { get; set; }

    public string Path { get; set; } = null!;
    public int StatusCode { get; set; }

    /// <summary>Console (<c>/admin/v1/*</c>) amali — Agentics operatori bajargan.</summary>
    public bool IsPlatformAction { get; set; }

    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
