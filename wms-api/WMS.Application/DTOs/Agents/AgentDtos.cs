using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Agents;

public class AgentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }
    public bool IsActive { get; set; }
    public bool PortalEnabled { get; set; }
    public string? PortalPhone { get; set; }
    // Quick summary (excludes cancelled records)
    public int SalesCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal CommissionDue { get; set; }
}

public class CreateAgentDto
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PortalEnabled { get; set; }
    public string? PortalPhone { get; set; }
    public string? PortalPassword { get; set; }
}

public class UpdateAgentDto
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PortalEnabled { get; set; }
    public string? PortalPhone { get; set; }
    public string? PortalPassword { get; set; }
}

public class CommissionRecordDto
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public string AgentName { get; set; } = null!;
    public int TransferId { get; set; }
    public string? CounterpartyName { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal CommissionAmount { get; set; }
    public CommissionStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateCommissionStatusDto
{
    public CommissionStatus Status { get; set; }
}

public class AgentSalesReportDto
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = null!;
    public decimal CommissionPercent { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCommission { get; set; }      // all non-cancelled
    public decimal CommissionConfirmed { get; set; }  // client paid
    public decimal CommissionPending { get; set; }    // client not paid yet (credit)
    public decimal CommissionCancelled { get; set; }  // returned / reversed
    public decimal CommissionPaid { get; set; }       // paid out to agent
    public decimal CommissionDue { get; set; }        // TotalCommission - CommissionPaid
    public List<AgentSalesPointDto> Timeline { get; set; } = new();
}

// One day of agent sales (for charts)
public class AgentSalesPointDto
{
    public DateTime Date { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal CommissionAmount { get; set; }
}

public class PayCommissionDto
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public bool RecordAsExpense { get; set; } = true;
}

// ── Agent portal (self-service cabinet) ──

public class AgentPortalLoginDto
{
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AgentPortalAuthResponseDto
{
    public string Token { get; set; } = null!;
    public AgentPortalProfileDto Agent { get; set; } = null!;
}

public class AgentPortalProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }
    public int TenantId { get; set; }
}
