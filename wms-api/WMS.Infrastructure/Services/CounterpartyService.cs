using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class CounterpartyService : ICounterpartyService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    public CounterpartyService(WmsDbContext db, ITenantStateService tenantState)
    { _db = db; _tenantState = tenantState; }

    public async Task<List<CounterpartyDto>> GetAllAsync(int tenantId, CounterpartyType? type = null)
    {
        await EnsureCounterpartyTypeAllowedAsync(tenantId, type);

        var q = _db.Counterparties.Where(c => c.TenantId == tenantId);
        if (type.HasValue) q = q.Where(c => c.Type == type.Value);

        var list = await q.Select(c => new CounterpartyDto
        {
            Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
            Address = c.Address, Note = c.Note, AgentId = c.AgentId,
            AgentName = c.Agent != null ? c.Agent.Name : null,
            PortalEnabled = c.PortalEnabled, PortalPhone = c.PortalPhone,
            Inn = c.Inn, OrganizationId = c.OrganizationId
        }).ToListAsync();

        await MarkPlatformTenantsAsync(list);
        return list;
    }

    /// <summary>
    /// Suppliers and clients are sold separately (`counterparties.suppliers` /
    /// `counterparties.clients`), and the list endpoint is filtered by type, so the check
    /// belongs here rather than in an attribute. Asking for "both" needs either feature.
    /// </summary>
    private async Task EnsureCounterpartyTypeAllowedAsync(int tenantId, CounterpartyType? type)
    {
        var state = await _tenantState.GetAsync(tenantId);
        if (state == null) return;

        bool suppliers = state.EnabledFeatures.Contains(FeatureCodes.CounterpartiesSuppliers);
        bool clients = state.EnabledFeatures.Contains(FeatureCodes.CounterpartiesClients);

        switch (type)
        {
            case CounterpartyType.Supplier when !suppliers:
                throw new FeatureDisabledException(FeatureCodes.CounterpartiesSuppliers);
            case CounterpartyType.Client when !clients:
                throw new FeatureDisabledException(FeatureCodes.CounterpartiesClients);
            case null or CounterpartyType.Both when !suppliers && !clients:
                throw new FeatureDisabledException(FeatureCodes.CounterpartiesClients);
        }
    }

    /// <summary>
    /// Marks the rows whose company already runs its own system on the platform. Only a
    /// boolean crosses the tenant boundary — never the other tenant's name or data.
    /// </summary>
    private async Task MarkPlatformTenantsAsync(List<CounterpartyDto> list)
    {
        var orgIds = list.Where(c => c.OrganizationId != null)
            .Select(c => c.OrganizationId!.Value).Distinct().ToList();
        if (orgIds.Count == 0) return;

        var tenantOrgIds = await _db.Tenants.AsNoTracking()
            .Where(t => t.OrganizationId != null && orgIds.Contains(t.OrganizationId.Value))
            .Select(t => t.OrganizationId!.Value)
            .Distinct()
            .ToListAsync();
        if (tenantOrgIds.Count == 0) return;

        var set = tenantOrgIds.ToHashSet();
        foreach (var c in list)
            if (c.OrganizationId != null && set.Contains(c.OrganizationId.Value))
                c.IsPlatformTenant = true;
    }

    public async Task<CounterpartyDto> GetByIdAsync(int tenantId, int id)
    {
        var dto = await _db.Counterparties
            .Where(c => c.Id == id && c.TenantId == tenantId)
            .Select(c => new CounterpartyDto
            {
                Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
                Address = c.Address, Note = c.Note, AgentId = c.AgentId,
                AgentName = c.Agent != null ? c.Agent.Name : null,
                PortalEnabled = c.PortalEnabled, PortalPhone = c.PortalPhone,
                Inn = c.Inn, OrganizationId = c.OrganizationId
            }).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Counterparty not found");

        var one = new List<CounterpartyDto> { dto };
        await MarkPlatformTenantsAsync(one);
        return dto;
    }

    public async Task<CounterpartyDto> CreateAsync(int tenantId, CreateCounterpartyDto dto)
    {
        await EnsureCounterpartyTypeAllowedAsync(tenantId, dto.Type);
        await ValidateAgentAsync(tenantId, dto.AgentId);

        // One company, one platform identity: an INN links this record to the shared
        // Organization so tenant A's "Ice Gold" and tenant B's "ICE GOLD MChJ" are the same firm.
        var organization = await OrganizationMatcher.ResolveAsync(
            _db, dto.Inn, dto.Name, PhoneHelper.Normalize(dto.Phone), dto.Address);

        var c = new Counterparty
        {
            TenantId = tenantId, Name = dto.Name, Type = dto.Type, Phone = PhoneHelper.Normalize(dto.Phone),
            Inn = organization?.Inn, OrganizationId = organization?.Id,
            Address = dto.Address, Note = dto.Note, AgentId = dto.AgentId, PortalEnabled = dto.PortalEnabled,
            PortalPhone = PhoneHelper.Normalize(dto.PortalPhone),
            PortalPasswordHash = !string.IsNullOrEmpty(dto.PortalPassword)
                ? BCrypt.Net.BCrypt.HashPassword(dto.PortalPassword) : null
        };
        _db.Counterparties.Add(c);
        await _db.SaveChangesAsync();
        return MapToDto(c);
    }

    public async Task<CounterpartyDto> UpdateAsync(int tenantId, int id, UpdateCounterpartyDto dto)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Counterparty not found");
        await ValidateAgentAsync(tenantId, dto.AgentId);
        var organization = await OrganizationMatcher.ResolveAsync(
            _db, dto.Inn, dto.Name, PhoneHelper.Normalize(dto.Phone), dto.Address);
        if (organization != null)
        {
            c.Inn = organization.Inn;
            c.OrganizationId = organization.Id;
        }
        else if (string.IsNullOrWhiteSpace(dto.Inn))
        {
            // INN cleared → unlink, but the Organization itself stays (other tenants may use it).
            c.Inn = null;
            c.OrganizationId = null;
        }

        c.Name = dto.Name; c.Type = dto.Type; c.Phone = PhoneHelper.Normalize(dto.Phone);
        c.Address = dto.Address; c.Note = dto.Note; c.AgentId = dto.AgentId; c.PortalEnabled = dto.PortalEnabled;
        c.PortalPhone = PhoneHelper.Normalize(dto.PortalPhone);
        if (!string.IsNullOrEmpty(dto.PortalPassword))
            c.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PortalPassword);
        await _db.SaveChangesAsync();
        return MapToDto(c);
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Counterparty not found");
        c.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<CounterpartyBalanceDto> GetBalanceAsync(int tenantId, int counterpartyId)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == counterpartyId && x.TenantId == tenantId)
            ?? throw new NotFoundException("Counterparty not found");
        var debt = await _db.Debts
            .Where(d => d.TenantId == tenantId && d.CounterpartyId == counterpartyId)
            .Select(d => d.Amount).FirstOrDefaultAsync();
        return new CounterpartyBalanceDto
        {
            CounterpartyId = c.Id, CounterpartyName = c.Name, DebtAmount = debt
        };
    }

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(int tenantId, int counterpartyId)
    {
        return await _db.PaymentHistories
            .Where(p => p.TenantId == tenantId && p.CounterpartyId == counterpartyId)
            .Include(p => p.Counterparty).Include(p => p.RecordedByUser)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentHistoryDto
            {
                Id = p.Id, CounterpartyId = p.CounterpartyId,
                CounterpartyName = p.Counterparty.Name, TransferId = p.TransferId,
                Amount = p.Amount, Method = p.Method, PaidAt = p.PaidAt,
                Note = p.Note, RecordedByUserName = p.RecordedByUser.FullName
            }).ToListAsync();
    }

    private async Task ValidateAgentAsync(int tenantId, int? agentId)
    {
        if (!agentId.HasValue) return;
        var agentExists = await _db.Agents.AnyAsync(a => a.Id == agentId.Value && a.TenantId == tenantId);
        if (!agentExists) throw new NotFoundException("Agent not found");
    }

    private static CounterpartyDto MapToDto(Counterparty c) => new()
    {
        Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
        Address = c.Address, Note = c.Note, AgentId = c.AgentId, PortalEnabled = c.PortalEnabled,
        PortalPhone = c.PortalPhone, Inn = c.Inn, OrganizationId = c.OrganizationId
    };
}
