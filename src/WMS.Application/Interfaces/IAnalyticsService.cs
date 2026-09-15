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

    /// <summary>
    /// Mahsulot bo'yicha foyda (P2.5): tasdiqlangan CHIQIM hujjatlari bo'yicha tushum va
    /// sotilgan partiyalarning tannarxi.
    /// </summary>
    /// <param name="from">Davr boshi (hujjat sanasi; kun UTC'da olinadi, chegara KIRADI).</param>
    /// <param name="to">Davr oxiri (o'sha kunning hujjatlari ham KIRADI).</param>
    /// <param name="productId">Bitta mahsulot; <see langword="null"/> — hammasi.</param>
    /// <returns>
    /// Foyda bo'yicha kamayish tartibidagi qatorlar. ⚠️ Tannarxi noma'lum qatorlar
    /// <c>Cost</c> ga 0 deb QO'SHILMAYDI — ular <c>UnknownCostQuantity</c> da ko'rinadi
    /// (sabab <see cref="ProductProfitDto"/> izohida).
    /// </returns>
    Task<List<ProductProfitDto>> GetProductProfit(DateTime from, DateTime to, Guid? productId = null);
}
