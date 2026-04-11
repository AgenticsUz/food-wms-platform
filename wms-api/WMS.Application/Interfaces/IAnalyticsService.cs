using WMS.Application.DTOs.Analytics;

namespace WMS.Application.Interfaces;

public interface IAnalyticsService
{
    Task<DashboardSummaryDto> GetDashboardSummary(int tenantId);
    Task<List<PlanVsActualDto>> GetProductionPlanVsActual(int tenantId, int days);
    Task<List<WasteByStageDto>> GetWasteByStage(int tenantId, int days);
    Task<List<DailyTransferDto>> GetDailyTransfers(int tenantId, int days);
    Task<List<StockLevelDto>> GetStockLevels(int tenantId, int? warehouseId);
    Task<List<StockHistoryDto>> GetStockHistory(int tenantId, int days);
    Task<List<IncomeExpenseDto>> GetIncomeExpense(int tenantId, int days);
    Task<List<TopDebtorDto>> GetTopDebtors(int tenantId, int top);
    Task<List<ShiftEfficiencyDto>> GetShiftEfficiency(int tenantId, int days);
    Task<List<AttendanceHeatmapDto>> GetAttendanceHeatmap(int tenantId, int days);
    Task<List<ProductDistributionDto>> GetProductDistribution(int tenantId);
    Task<ExtendedDashboardSummaryDto> GetExtendedDashboardSummary(int tenantId, DateTime? fromDate, DateTime? toDate);
    Task<List<MonthlyComparisonDto>> GetMonthlyComparison(int tenantId);
}
