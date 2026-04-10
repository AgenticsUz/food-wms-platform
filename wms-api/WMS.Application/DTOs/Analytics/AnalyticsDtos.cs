using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Analytics;

public class DashboardSummaryDto
{
    public decimal TotalStockKg { get; set; }
    public int ActiveProductionOrders { get; set; }
    public int PendingTransfers { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public int LowStockProductCount { get; set; }
    public decimal OverallEfficiencyPercent { get; set; }
    public decimal TotalDebt { get; set; }
}

public class PlanVsActualDto
{
    public DateTime Date { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal Planned { get; set; }
    public decimal Actual { get; set; }
    public decimal EfficiencyPercent => Planned > 0 ? Math.Round(Actual / Planned * 100, 1) : 0;
}

public class WasteByStageDto
{
    public string StageName { get; set; } = null!;
    public decimal TotalActual { get; set; }
    public decimal TotalWaste { get; set; }
    public decimal WastePercent => TotalActual > 0 ? Math.Round(TotalWaste / TotalActual * 100, 2) : 0;
}

public class DailyTransferDto
{
    public DateTime Date { get; set; }
    public decimal IncomingTotal { get; set; }
    public decimal OutgoingTotal { get; set; }
    public int IncomingCount { get; set; }
    public int OutgoingCount { get; set; }
}

public class StockLevelDto
{
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
    public bool IsLow => CurrentStock <= MinStock;
}

public class StockHistoryDto
{
    public DateTime Date { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = null!;
}

public class IncomeExpenseDto
{
    public DateTime Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net => Income - Expense;
}

public class TopDebtorDto
{
    public string CounterpartyName { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public decimal DebtAmount { get; set; }
}

public class ShiftEfficiencyDto
{
    public string ShiftName { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public decimal EfficiencyPercent => PlannedQty > 0 ? Math.Round(ActualQty / PlannedQty * 100, 1) : 0;
}

public class AttendanceHeatmapDto
{
    public string WorkerName { get; set; } = null!;
    public DateTime Date { get; set; }
    public int HoursWorked { get; set; }
    public bool WasPresent { get; set; }
}

public class ProductDistributionDto
{
    public string ProductName { get; set; } = null!;
    public ProductType Type { get; set; }
    public decimal TotalStock { get; set; }
    public decimal Percentage { get; set; }
}
