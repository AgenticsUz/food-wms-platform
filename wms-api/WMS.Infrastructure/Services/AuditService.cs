using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Audit;
using WMS.Application.DTOs.Common;
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

    /// <inheritdoc />
    public async Task<PaginatedList<AuditLogDto>> GetPlatformLogsAsync(AdminAuditQuery query,
        CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.TenantId is { } tenantId) q = q.Where(a => a.TenantId == tenantId);
        if (query.UserId is { } userId) q = q.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.Action)) q = q.Where(a => a.Action == query.Action);
        if (query.PlatformOnly == true) q = q.Where(a => a.IsPlatformAction);
        if (query.From is { } from) q = q.Where(a => a.CreatedAt >= from);
        if (query.To is { } to) q = q.Where(a => a.CreatedAt <= to);

        var total = await q.CountAsync(ct);

        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        // Tenant nomlari alohida so'rov bilan olinadi: audit jadvalida Tenant navigatsiyasi
        // yo'q (append-only, hech qanday bog'lanishsiz) va har qator uchun join qilish
        // o'sib borgan jadvalda qimmatga tushadi. Sahifa 200 qatordan oshmaydi.
        var rows = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id, UserId = a.UserId, UserName = a.UserName,
                Action = a.Action, EntityType = a.EntityType, EntityAction = a.EntityAction,
                EntityId = a.EntityId, Path = a.Path, StatusCode = a.StatusCode, CreatedAt = a.CreatedAt,
                IsPlatformAction = a.IsPlatformAction,
                TenantId = a.TenantId, ActorTenantId = a.ActorTenantId
            })
            .ToListAsync(ct);

        var ids = rows.Select(r => r.TenantId)
            .Concat(rows.Where(r => r.ActorTenantId.HasValue).Select(r => r.ActorTenantId!.Value))
            .Distinct().ToList();

        // `IgnoreQueryFilters` — o'chirilgan tenantning izi ham o'qilishi kerak,
        // aks holda audit jurnalida nomsiz qatorlar paydo bo'ladi.
        var names = await _db.Tenants.AsNoTracking().IgnoreQueryFilters()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        foreach (var r in rows)
        {
            r.TenantName = names.GetValueOrDefault(r.TenantId);
            if (r.ActorTenantId is { } actorId) r.ActorTenantName = names.GetValueOrDefault(actorId);
        }

        return new PaginatedList<AuditLogDto>
        {
            Items = rows, Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    /// <inheritdoc />
    public async Task<List<string>> GetPlatformEntityTypesAsync(CancellationToken ct = default)
        => await _db.AuditLogs.AsNoTracking()
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
}
