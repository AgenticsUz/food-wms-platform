using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Counterparties;

public class CounterpartyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public bool PortalEnabled { get; set; }
    public string? PortalPhone { get; set; }
    public string? Inn { get; set; }
    public int? OrganizationId { get; set; }
    /// True when the linked Organization is itself a tenant of the platform — the little
    /// green marker in the list. Read-only signal for now; the partnership flow comes later.
    public bool IsPlatformTenant { get; set; }
}

public class CreateCounterpartyDto
{
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    /// Taxpayer id (STIR, 9 digits). Optional; when given it links this record to the
    /// platform-level Organization so the same firm is one identity across tenants.
    public string? Inn { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public int? AgentId { get; set; }
    public bool PortalEnabled { get; set; }
    public string? PortalPhone { get; set; }
    public string? PortalPassword { get; set; }
}

public class UpdateCounterpartyDto
{
    public string Name { get; set; } = null!;
    public string? Inn { get; set; }
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public int? AgentId { get; set; }
    public bool PortalEnabled { get; set; }
    public string? PortalPhone { get; set; }
    public string? PortalPassword { get; set; }
}

public class CounterpartyBalanceDto
{
    public int CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public decimal DebtAmount { get; set; }
}
