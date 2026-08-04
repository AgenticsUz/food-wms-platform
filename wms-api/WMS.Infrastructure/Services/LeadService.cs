using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class LeadService : ILeadService
{
    /// A repeat request from the same phone inside this window updates the existing lead
    /// instead of creating another one, so a customer clicking twice does not become two rows.
    private static readonly TimeSpan PublicDedupeWindow = TimeSpan.FromHours(24);
    /// Portal users get a longer window: they see the banner on every visit.
    private static readonly TimeSpan PortalDedupeWindow = TimeSpan.FromDays(30);

    private readonly WmsDbContext _db;
    private readonly ITenantService _tenants;
    private readonly SubscriptionOptions _options;

    public LeadService(WmsDbContext db, ITenantService tenants, IOptions<SubscriptionOptions> options)
    {
        _db = db;
        _tenants = tenants;
        _options = options.Value;
    }

    public async Task<LeadDto> SubmitAsync(CreateLeadDto dto, LeadSource source)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName)) throw new AppException("Company name is required");
        if (string.IsNullOrWhiteSpace(dto.ContactName)) throw new AppException("Contact name is required");

        var phone = PhoneHelper.Normalize(dto.Phone)
            ?? throw new AppException("Phone is required");

        var since = DateTime.UtcNow - PublicDedupeWindow;
        var recent = await _db.Leads
            .Where(l => l.Phone == phone && l.CreatedAt >= since)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync();

        if (recent != null)
        {
            // Answer 200 with the existing lead: the visitor sees success, we get no spam.
            if (!string.IsNullOrWhiteSpace(dto.Note)) recent.Note = dto.Note;
            await _db.SaveChangesAsync();
            return await MapAsync(recent);
        }

        var lead = new Lead
        {
            CompanyName = dto.CompanyName.Trim(),
            ContactName = dto.ContactName.Trim(),
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Note = dto.Note,
            Source = source,
            Status = LeadStatus.New
        };
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        return await MapAsync(lead);
    }

    public async Task<List<LeadDto>> GetAllAsync(LeadStatus? status, LeadSource? source,
        string? search, int page, int pageSize)
    {
        var query = _db.Leads.AsNoTracking().AsQueryable();

        if (status.HasValue) query = query.Where(l => l.Status == status.Value);
        if (source.HasValue) query = query.Where(l => l.Source == source.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l => l.CompanyName.Contains(term)
                                     || l.ContactName.Contains(term)
                                     || l.Phone.Contains(term));
        }

        var leads = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<LeadDto>(leads.Count);
        foreach (var lead in leads) result.Add(await MapAsync(lead));
        return result;
    }

    public async Task<LeadDto> GetByIdAsync(int id)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Lead not found");
        return await MapAsync(lead);
    }

    public async Task<LeadDto> UpdateAsync(int id, UpdateLeadDto dto)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Lead not found");

        lead.Status = dto.Status;
        lead.StatusNote = dto.StatusNote;
        await _db.SaveChangesAsync();
        return await MapAsync(lead);
    }

    public async Task<TenantDto> ConvertAsync(int id, ConvertLeadDto dto)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Lead not found");

        if (lead.ConvertedTenantId != null)
            throw new AppException("This lead has already been converted");

        // Provisioning validates the slug and throws AppException (400) on a clash — the lead
        // is left untouched so the operator can simply retry with another slug.
        var tenant = await _tenants.CreateAsync(new CreateTenantDto
        {
            Name = dto.Name,
            Slug = dto.Slug,
            AdminFullName = dto.AdminFullName,
            AdminPhone = dto.AdminPhone,
            AdminPassword = dto.AdminPassword,
            PlanId = dto.PlanId,
            Inn = dto.Inn,
            // Provisioning applies the paid period itself, so the response the console shows
            // already carries the date instead of a stale "trial" state.
            PaidUntil = dto.PaidUntil
        });

        lead.Status = LeadStatus.Won;
        lead.ConvertedTenantId = tenant.Id;
        await _db.SaveChangesAsync();

        return tenant;
    }

    public async Task<LeadDto> SubmitPortalInterestAsync(int tenantId, int? counterpartyId, int? agentId,
        string companyName, string contactName, UpgradeInterestDto dto)
    {
        var existing = await FindPortalLeadAsync(tenantId, counterpartyId, agentId);
        if (existing != null) return await MapAsync(existing);

        var phone = PhoneHelper.Normalize(dto.Phone);
        if (phone == null)
            throw new AppException("Phone is required so we can call you back");

        var referrerName = await _db.Tenants.Where(t => t.Id == tenantId)
            .Select(t => t.Name).FirstOrDefaultAsync();

        var lead = new Lead
        {
            CompanyName = string.IsNullOrWhiteSpace(companyName) ? contactName : companyName.Trim(),
            ContactName = contactName,
            Phone = phone,
            Note = string.Join(" · ", new[]
            {
                dto.Note,
                $"From the portal of {referrerName} (tenant #{tenantId})"
            }.Where(x => !string.IsNullOrWhiteSpace(x))),
            Source = LeadSource.Portal,
            ReferrerTenantId = tenantId,
            SourceCounterpartyId = counterpartyId,
            SourceAgentId = agentId,
            Status = LeadStatus.New
        };
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        return await MapAsync(lead);
    }

    public async Task<UpgradeInterestStatusDto> GetPortalInterestAsync(int tenantId, int? counterpartyId, int? agentId)
    {
        var lead = await FindPortalLeadAsync(tenantId, counterpartyId, agentId);
        return new UpgradeInterestStatusDto
        {
            Submitted = lead != null,
            SubmittedAt = lead?.CreatedAt,
            Status = lead?.Status
        };
    }

    private async Task<Lead?> FindPortalLeadAsync(int tenantId, int? counterpartyId, int? agentId)
    {
        var since = DateTime.UtcNow - PortalDedupeWindow;
        return await _db.Leads
            .Where(l => l.ReferrerTenantId == tenantId
                        && l.CreatedAt >= since
                        && ((counterpartyId != null && l.SourceCounterpartyId == counterpartyId)
                            || (agentId != null && l.SourceAgentId == agentId)))
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<LeadDto> MapAsync(Lead lead)
    {
        string? referrer = null;
        if (lead.ReferrerTenantId is { } tid)
            referrer = await _db.Tenants.Where(t => t.Id == tid).Select(t => t.Name).FirstOrDefaultAsync();

        return new LeadDto
        {
            Id = lead.Id, CompanyName = lead.CompanyName, ContactName = lead.ContactName,
            Phone = lead.Phone, Email = lead.Email, Note = lead.Note,
            Source = lead.Source, ReferrerTenantId = lead.ReferrerTenantId, ReferrerTenantName = referrer,
            Status = lead.Status, StatusNote = lead.StatusNote,
            ConvertedTenantId = lead.ConvertedTenantId,
            CreatedAt = lead.CreatedAt, UpdatedAt = lead.UpdatedAt
        };
    }
}
