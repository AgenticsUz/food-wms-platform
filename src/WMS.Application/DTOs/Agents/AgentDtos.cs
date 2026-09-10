using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Agents;

// ⚠️ F6 da O'CHDI: agent portali (login, profil, o'z hisoboti) va `Portal*` maydonlari (D8) —
// Identity'dan tashqari ikkinchi login reyestri. 2-bosqichda Identity'ning `agent` roli bilan qaytadi.

public class AgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }
    public bool IsActive { get; set; }
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
}

public class UpdateAgentDto
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal CommissionPercent { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CommissionRecordDto
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = null!;
    public Guid TransferId { get; set; }
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
    public Guid AgentId { get; set; }
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
