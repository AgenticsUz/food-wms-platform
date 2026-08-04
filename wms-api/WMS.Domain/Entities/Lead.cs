using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// A demo request / sales lead. Platform-level: there is no owning tenant, because a lead
/// exists before any tenant does. <see cref="ReferrerTenantId"/> records which existing
/// tenant's portal the lead came through, so referrals can be credited later.
/// </summary>
public class Lead : BaseEntity
{
    public string CompanyName { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Note { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;
    public int? ReferrerTenantId { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.New;
    public string? StatusNote { get; set; }

    /// Set once the lead becomes a paying tenant.
    public int? ConvertedTenantId { get; set; }

    /// Set when the lead came from a counterparty/agent portal (S7), so a repeat request
    /// from the same portal user can be recognised instead of creating a duplicate.
    public int? SourceCounterpartyId { get; set; }
    public int? SourceAgentId { get; set; }
}
