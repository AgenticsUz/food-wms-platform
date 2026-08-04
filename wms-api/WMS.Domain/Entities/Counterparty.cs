using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Counterparty : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public int? AgentId { get; set; }          // default agent for this client
    public Agent? Agent { get; set; }
    /// Taxpayer id (STIR, 9 digits). Optional, but when present it links this record to the
    /// platform-level Organization so the same company is one identity across tenants.
    public string? Inn { get; set; }
    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string? PortalPhone { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool PortalEnabled { get; set; } = false;
}
