using WMS.Application.DTOs.Audit;

namespace WMS.Application.Interfaces;

/// <remarks>
/// Faqat tenantning o'z jurnali. SQLite davridagi platforma ko'rinishi
/// (<c>GetPlatformLogsAsync</c>, <c>GetPlatformEntityTypesAsync</c>) O'CHDI: <c>audit_log</c> RLS
/// ostida va tenantsiz o'qib bo'lmaydi — Console <c>/admin/v1/audit</c> dan tenantni oshkora tanlaydi.
/// </remarks>
public interface IAuditService
{
    Task<List<AuditLogDto>> GetLogsAsync(string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize);

    Task<List<string>> GetEntityTypesAsync();
}
