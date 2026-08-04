using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
/// <remarks>
/// SuperAdmin-only by construction: it deliberately exposes cross-tenant information
/// (which tenants know the same company), which no tenant may ever see.
/// </remarks>
public class OrganizationService : IOrganizationService
{
    private readonly WmsDbContext _db;
    public OrganizationService(WmsDbContext db) => _db = db;

    public async Task<List<OrganizationDto>> SearchAsync(string? search, int page, int pageSize)
    {
        var query = _db.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.Name.Contains(term) || (o.Inn != null && o.Inn.Contains(term)));
        }

        var organizations = await query
            .OrderBy(o => o.Name)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ids = organizations.Select(o => o.Id).ToList();
        var tenantCounts = await _db.Tenants
            .Where(t => t.OrganizationId != null && ids.Contains(t.OrganizationId.Value))
            .GroupBy(t => t.OrganizationId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
        var counterpartyCounts = await _db.Counterparties
            .Where(c => c.OrganizationId != null && ids.Contains(c.OrganizationId.Value))
            .GroupBy(c => c.OrganizationId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return organizations.Select(o => new OrganizationDto
        {
            Id = o.Id, Name = o.Name, Inn = o.Inn, Phone = o.Phone, Address = o.Address,
            TenantCount = tenantCounts.GetValueOrDefault(o.Id, 0),
            CounterpartyCount = counterpartyCounts.GetValueOrDefault(o.Id, 0),
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<OrganizationDetailDto> GetByIdAsync(int id)
    {
        var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException("Organization not found");

        var tenants = await _db.Tenants.AsNoTracking()
            .Where(t => t.OrganizationId == id)
            .Select(t => new OrganizationTenantDto { Id = t.Id, Name = t.Name, Slug = t.Slug })
            .ToListAsync();

        var counterparties = await _db.Counterparties.AsNoTracking()
            .Where(c => c.OrganizationId == id)
            .Join(_db.Tenants.AsNoTracking(), c => c.TenantId, t => t.Id, (c, t) => new OrganizationCounterpartyDto
            {
                Id = c.Id, TenantId = t.Id, TenantName = t.Name, CounterpartyName = c.Name
            })
            .ToListAsync();

        return new OrganizationDetailDto
        {
            Id = organization.Id, Name = organization.Name, Inn = organization.Inn,
            Phone = organization.Phone, Address = organization.Address,
            CreatedAt = organization.CreatedAt,
            TenantCount = tenants.Count, CounterpartyCount = counterparties.Count,
            Tenants = tenants, Counterparties = counterparties
        };
    }
}
