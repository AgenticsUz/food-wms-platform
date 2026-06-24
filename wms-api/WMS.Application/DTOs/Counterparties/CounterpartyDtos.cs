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
}

public class CreateCounterpartyDto
{
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
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
