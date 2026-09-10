using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Counterparties;

// ⚠️ F6 da O'CHDI: `PortalEnabled`/`PortalPhone`/`PortalPassword` (kontragent portali, D8) va
// `OrganizationId`/`IsPlatformTenant` (global INN katalogi, D10). INN kontragentning o'zida qoladi.

public class CounterpartyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? Inn { get; set; }
}

public class CreateCounterpartyDto
{
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    /// Taxpayer id (STIR, 9 digits). Optional; spaces and dashes are ignored.
    public string? Inn { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public Guid? AgentId { get; set; }
}

public class UpdateCounterpartyDto
{
    public string Name { get; set; } = null!;
    public string? Inn { get; set; }
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public Guid? AgentId { get; set; }
}

public class CounterpartyBalanceDto
{
    public Guid CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public decimal DebtAmount { get; set; }
}
