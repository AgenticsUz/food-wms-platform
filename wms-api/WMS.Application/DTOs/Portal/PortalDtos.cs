using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Portal;

public class PortalLoginDto
{
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class PortalAuthResponseDto
{
    public string Token { get; set; } = null!;
    public PortalCounterpartyDto Counterparty { get; set; } = null!;
}

public class PortalCounterpartyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public int TenantId { get; set; }
}

public class PortalFinanceDto
{
    public decimal Balance { get; set; }
    public decimal TotalDebt { get; set; }
}
