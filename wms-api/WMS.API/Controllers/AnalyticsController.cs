using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Analytics;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("dashboard.view")]
public class AnalyticsController : BaseController
{
    private readonly IAnalyticsService _analytics;
    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    // Umumiy dashboard ko'rsatkichlari ataylab gate qilinmagan: bu tenantning O'Z
    // yig'ma raqamlari (bitta karta), modul sahifasi emas. Modulga xos analitika
    // (production/finance/kpi/warehouse/transfers) esa quyida modul bo'yicha yopilgan.
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(ApiResponse<DashboardSummaryDto>.Ok(await _analytics.GetDashboardSummary(TenantId)));

    [HttpGet("production/plan-vs-actual")]
    [RequireModule(ModuleCodes.Production)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetProductionPlanVsActual([FromQuery] int days = 7)
        => Ok(ApiResponse<List<PlanVsActualDto>>.Ok(await _analytics.GetProductionPlanVsActual(TenantId, days)));

    [HttpGet("production/waste-by-stage")]
    [RequireModule(ModuleCodes.Production)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetWasteByStage([FromQuery] int days = 30)
        => Ok(ApiResponse<List<WasteByStageDto>>.Ok(await _analytics.GetWasteByStage(TenantId, days)));

    [HttpGet("transfers/daily")]
    [RequireModule(ModuleCodes.Transfers)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetDailyTransfers([FromQuery] int days = 7)
        => Ok(ApiResponse<List<DailyTransferDto>>.Ok(await _analytics.GetDailyTransfers(TenantId, days)));

    [HttpGet("warehouse/stock-levels")]
    [RequireModule(ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetStockLevels([FromQuery] int? warehouseId)
        => Ok(ApiResponse<List<StockLevelDto>>.Ok(await _analytics.GetStockLevels(TenantId, warehouseId)));

    [HttpGet("warehouse/stock-history")]
    [RequireModule(ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetStockHistory([FromQuery] int days = 30)
        => Ok(ApiResponse<List<StockHistoryDto>>.Ok(await _analytics.GetStockHistory(TenantId, days)));

    [HttpGet("finance/income-expense")]
    [RequireModule(ModuleCodes.Finance)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetIncomeExpense([FromQuery] int days = 30)
        => Ok(ApiResponse<List<IncomeExpenseDto>>.Ok(await _analytics.GetIncomeExpense(TenantId, days)));

    [HttpGet("finance/top-debtors")]
    [RequireModule(ModuleCodes.Finance)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetTopDebtors([FromQuery] int top = 10)
        => Ok(ApiResponse<List<TopDebtorDto>>.Ok(await _analytics.GetTopDebtors(TenantId, top)));

    [HttpGet("kpi/shift-efficiency")]
    [RequireModule(ModuleCodes.Kpi)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetShiftEfficiency([FromQuery] int days = 7)
        => Ok(ApiResponse<List<ShiftEfficiencyDto>>.Ok(await _analytics.GetShiftEfficiency(TenantId, days)));

    [HttpGet("kpi/attendance-heatmap")]
    [RequireModule(ModuleCodes.Kpi)]
    [RequireFeature(FeatureCodes.AnalyticsAdvanced)]
    public async Task<IActionResult> GetAttendanceHeatmap([FromQuery] int days = 30)
        => Ok(ApiResponse<List<AttendanceHeatmapDto>>.Ok(await _analytics.GetAttendanceHeatmap(TenantId, days)));

    [HttpGet("products/distribution")]
    public async Task<IActionResult> GetProductDistribution()
        => Ok(ApiResponse<List<ProductDistributionDto>>.Ok(await _analytics.GetProductDistribution(TenantId)));

    [HttpGet("dashboard-summary")]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        => Ok(ApiResponse<ExtendedDashboardSummaryDto>.Ok(
            await _analytics.GetExtendedDashboardSummary(TenantId, fromDate, toDate)));

    [HttpGet("monthly-comparison")]
    public async Task<IActionResult> GetMonthlyComparison()
        => Ok(ApiResponse<List<MonthlyComparisonDto>>.Ok(
            await _analytics.GetMonthlyComparison(TenantId)));
}
