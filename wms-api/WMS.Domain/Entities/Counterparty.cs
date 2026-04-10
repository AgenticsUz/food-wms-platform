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
    public string? PortalPhone { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool PortalEnabled { get; set; } = false;
}
