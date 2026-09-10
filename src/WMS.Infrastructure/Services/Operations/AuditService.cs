using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Audit;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations;

// F6: faqat joriy tenant jurnali (RLS). Platforma ko'rinishi (barcha tenantlar) o'chdi —
// Console `/admin/v1/audit` (ControlPlaneController) dan tenantni oshkora tanlab o'qiydi.
public class AuditService : IAuditService
{
    private const int DefaultPageSize = 50;

    /// Audit — eng tez o'sadigan jadval. Chegara xato emas, jimgina qisqartiriladi.
    private const int MaxPageSize = 200;

    private readonly WmsDbContext _db;
    public AuditService(WmsDbContext db) => _db = db;

    public async Task<List<AuditLogDto>> GetLogsAsync(string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        // SQLite manfiy OFFSET'ni jimgina 0 deb olardi, Postgres esa xato beradi (500).
        if (page < 1) page = 1;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var q = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.CreatedAt <= to.Value);

        return await q.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id, ActorSub = a.ActorSub, UserName = a.UserName,
                Action = a.Action, EntityType = a.EntityType, EntityAction = a.EntityAction,
                EntityId = a.EntityId, Path = a.Path, StatusCode = a.StatusCode, CreatedAt = a.CreatedAt,
                IsPlatformAction = a.IsPlatformAction
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetEntityTypesAsync()
    {
        return await _db.AuditLogs
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }
}
