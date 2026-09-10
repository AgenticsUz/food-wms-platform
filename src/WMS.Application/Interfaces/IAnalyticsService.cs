using WMS.Application.DTOs.Analytics;

namespace WMS.Application.Interfaces;

/// <remarks>
/// F6: <c>tenantId</c> parametrlari o'chdi (D4) — tenant global filtr va RLS'dan. Kun/oy
/// bo'yicha guruhlash UTC'da (bazada <c>timestamptz</c>), SQLite davridagidek.
/// </remarks>
public interface IAnalyticsService
{
    Task<DashboardSummaryDto> GetDashboardSummary();
    Task<List<PlanVsActualDto>> GetProductionPlanVsActual(int days);
    Task<List<WasteByStageDto>> GetWasteByStage(int days);
    Task<List<DailyTransferDto>> GetDailyTransfers(int days);
    Task<List<StockLevelDto>> GetStockLevels(Guid? warehouseId);
    Task<List<StockHistoryDto>> GetStockHistory(int days);
    Task<List<IncomeExpenseDto>> GetIncomeExpense(int days);
    Task<List<TopDebtorDto>> GetTopDebtors(int top);
    Task<List<ShiftEfficiencyDto>> GetShiftEfficiency(int days);
    Task<List<AttendanceHeatmapDto>> GetAttendanceHeatmap(int days);
    Task<List<ProductDistributionDto>> GetProductDistribution();
    Task<ExtendedDashboardSummaryDto> GetExtendedDashboardSummary(DateTime? fromDate, DateTime? toDate);
    Task<List<MonthlyComparisonDto>> GetMonthlyComparison();
}
