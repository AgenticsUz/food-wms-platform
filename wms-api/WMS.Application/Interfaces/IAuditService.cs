using WMS.Application.DTOs.Audit;

namespace WMS.Application.Interfaces;

public interface IAuditService
{
    Task<List<AuditLogDto>> GetLogsAsync(int tenantId, string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize);

    Task<List<string>> GetEntityTypesAsync(int tenantId);
}
