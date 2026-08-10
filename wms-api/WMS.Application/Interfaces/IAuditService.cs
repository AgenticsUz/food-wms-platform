using WMS.Application.DTOs.Audit;
using WMS.Application.DTOs.Common;

namespace WMS.Application.Interfaces;

public interface IAuditService
{
    Task<List<AuditLogDto>> GetLogsAsync(int tenantId, string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize);

    Task<List<string>> GetEntityTypesAsync(int tenantId);

    /// <summary>
    /// Platforma ko'rinishi — barcha tenantlar bo'yicha. Faqat SuperAdmin chaqiradi.
    /// Sahifalash majburiy: audit eng tez o'sadigan jadval.
    /// </summary>
    Task<PaginatedList<AuditLogDto>> GetPlatformLogsAsync(AdminAuditQuery query,
        CancellationToken ct = default);

    /// Platforma ko'rinishidagi "amal turi" filtri uchun — barcha tenantlar bo'yicha.
    Task<List<string>> GetPlatformEntityTypesAsync(CancellationToken ct = default);
}
