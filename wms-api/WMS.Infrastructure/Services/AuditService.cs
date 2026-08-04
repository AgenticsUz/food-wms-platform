using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Audit;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly WmsDbContext _db;
    public AuditService(WmsDbContext db) => _db = db;

    public async Task<List<AuditLogDto>> GetLogsAsync(int tenantId, string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        var q = _db.AuditLogs.Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.CreatedAt <= to.Value);

        return await q.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id, UserId = a.UserId, UserName = a.UserName,
                Action = a.Action, EntityType = a.EntityType, EntityAction = a.EntityAction,
                EntityId = a.EntityId, Path = a.Path, StatusCode = a.StatusCode, CreatedAt = a.CreatedAt,
                IsPlatformAction = a.IsPlatformAction
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetEntityTypesAsync(int tenantId)
    {
        return await _db.AuditLogs
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }
}
