using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Analytics;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly WmsDbContext _db;
    public AnalyticsService(WmsDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetDashboardSummary(int tenantId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var totalStock = await _db.WarehouseStocks
            .Where(s => s.TenantId == tenantId).SumAsync(s => (double)s.Quantity);
        var activeOrders = await _db.ProductionOrders
            .Where(o => o.TenantId == tenantId && o.Status == ProductionOrderStatus.InProgress).CountAsync();
        var pendingTransfers = await _db.Transfers
            .Where(t => t.TenantId == tenantId && t.Status == TransferStatus.Pending).CountAsync();
        var monthRevenue = await _db.Transactions
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Income && t.Date >= monthStart)
            .SumAsync(t => (double)t.Amount);
        var lastMonthRevenue = await _db.Transactions
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Income
                && t.Date >= lastMonthStart && t.Date < monthStart)
            .SumAsync(t => (double)t.Amount);
        var revenueChange = lastMonthRevenue > 0
            ? Math.Round(((decimal)monthRevenue - (decimal)lastMonthRevenue) / (decimal)lastMonthRevenue * 100, 1) : 0;
        var totalDebt = await _db.Debts
            .Where(d => d.TenantId == tenantId).SumAsync(d => (double)d.Amount);

        // Low stock count
        var products = await _db.Products.Where(p => p.TenantId == tenantId && p.MinStock > 0).ToListAsync();
        var lowStockCount = 0;
        foreach (var p in products)
        {
            var stock = await _db.WarehouseStocks
                .Where(s => s.TenantId == tenantId && s.ProductId == p.Id)
                .SumAsync(s => (double)s.Quantity);
            if ((decimal)stock <= p.MinStock) lowStockCount++;
        }

        // Efficiency last 7 days
        var from7 = now.AddDays(-7);
        var plans = await _db.ShiftPlans.Where(p => p.TenantId == tenantId && p.Date >= from7)
            .SumAsync(p => (double)p.PlannedQuantity);
        var actuals = await _db.ShiftActuals.Where(a => a.TenantId == tenantId && a.Date >= from7)
            .SumAsync(a => (double)a.ActualQuantity);

        return new DashboardSummaryDto
        {
            TotalStockKg = (decimal)totalStock, ActiveProductionOrders = activeOrders,
            PendingTransfers = pendingTransfers, MonthlyRevenue = (decimal)monthRevenue,
            RevenueChangePercent = revenueChange, LowStockProductCount = lowStockCount,
            OverallEfficiencyPercent = plans > 0 ? Math.Round((decimal)actuals / (decimal)plans * 100, 1) : 0,
            TotalDebt = (decimal)totalDebt
        };
    }

    public async Task<List<PlanVsActualDto>> GetProductionPlanVsActual(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var planList = await _db.ShiftPlans.Include(p => p.Product)
            .Where(p => p.TenantId == tenantId && p.Date >= from).ToListAsync();
        var actualList = await _db.ShiftActuals.Include(a => a.Product)
            .Where(a => a.TenantId == tenantId && a.Date >= from).ToListAsync();

        return planList
            .GroupBy(p => new { p.Date.Date, p.Product.Name })
            .Select(g => new PlanVsActualDto
            {
                Date = g.Key.Date, ProductName = g.Key.Name,
                Planned = g.Sum(p => p.PlannedQuantity),
                Actual = actualList
                    .Where(a => a.Date.Date == g.Key.Date && a.Product.Name == g.Key.Name)
                    .Sum(a => a.ActualQuantity)
            }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<WasteByStageDto>> GetWasteByStage(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        return await _db.StageExecutions
            .Include(se => se.RecipeStage).ThenInclude(rs => rs.Stage)
            .Include(se => se.ProductionOrder)
            .Where(se => se.ProductionOrder.TenantId == tenantId
                && se.Status == StageExecutionStatus.Completed
                && se.EndTime >= from)
            .GroupBy(se => se.RecipeStage.Stage.Name)
            .Select(g => new WasteByStageDto
            {
                StageName = g.Key,
                TotalActual = (decimal)g.Sum(se => (double)se.ActualQuantity),
                TotalWaste = (decimal)g.Sum(se => (double)se.WasteQuantity)
            }).ToListAsync();
    }

    public async Task<List<DailyTransferDto>> GetDailyTransfers(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var transfers = await _db.Transfers
            .Where(t => t.TenantId == tenantId && t.Status == TransferStatus.Confirmed && t.CreatedAt >= from)
            .Include(t => t.Items).ToListAsync();

        return transfers.GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyTransferDto
            {
                Date = g.Key,
                IncomingTotal = g.Where(t => t.Type == TransferType.Incoming)
                    .SelectMany(t => t.Items).Sum(i => i.Quantity * i.UnitPrice),
                OutgoingTotal = g.Where(t => t.Type == TransferType.Outgoing)
                    .SelectMany(t => t.Items).Sum(i => i.Quantity * i.UnitPrice),
                IncomingCount = g.Count(t => t.Type == TransferType.Incoming),
                OutgoingCount = g.Count(t => t.Type == TransferType.Outgoing)
            }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<StockLevelDto>> GetStockLevels(int tenantId, int? warehouseId)
    {
        var q = _db.WarehouseStocks.Where(s => s.TenantId == tenantId)
            .Include(s => s.Product).ThenInclude(p => p.Unit).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);

        var stocks = await q.ToListAsync();
        return stocks.GroupBy(s => new { s.ProductId, s.Product.Name, s.Product.Unit.ShortName, s.Product.MinStock })
            .Select(g => new StockLevelDto
            {
                ProductName = g.Key.Name, UnitShortName = g.Key.ShortName,
                CurrentStock = g.Sum(s => s.Quantity), MinStock = g.Key.MinStock
            }).ToList();
    }

    public async Task<List<StockHistoryDto>> GetStockHistory(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        return await _db.Transfers
            .Where(t => t.TenantId == tenantId && t.Status == TransferStatus.Confirmed && t.ConfirmedAt >= from)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .SelectMany(t => t.Items.Select(i => new StockHistoryDto
            {
                Date = t.ConfirmedAt!.Value,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                MovementType = t.Type == TransferType.Incoming || t.Type == TransferType.ProductionOutput
                    ? "Incoming" : "Outgoing"
            })).OrderBy(x => x.Date).ToListAsync();
    }

    public async Task<List<IncomeExpenseDto>> GetIncomeExpense(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var txs = await _db.Transactions
            .Where(t => t.TenantId == tenantId && t.Date >= from).ToListAsync();

        return txs.GroupBy(t => t.Date.Date).Select(g => new IncomeExpenseDto
        {
            Date = g.Key,
            Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<TopDebtorDto>> GetTopDebtors(int tenantId, int top)
    {
        return await _db.Debts.Where(d => d.TenantId == tenantId && d.Amount != 0)
            .Include(d => d.Counterparty)
            .OrderByDescending(d => Math.Abs(d.Amount))
            .Take(top)
            .Select(d => new TopDebtorDto
            {
                CounterpartyName = d.Counterparty.Name,
                Type = d.Counterparty.Type,
                DebtAmount = d.Amount
            }).ToListAsync();
    }

    public async Task<List<ShiftEfficiencyDto>> GetShiftEfficiency(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var plans = await _db.ShiftPlans.Include(p => p.Shift)
            .Where(p => p.TenantId == tenantId && p.Date >= from).ToListAsync();
        var actuals = await _db.ShiftActuals
            .Where(a => a.TenantId == tenantId && a.Date >= from).ToListAsync();

        return plans.GroupBy(p => new { p.Shift.Name, p.Date.Date })
            .Select(g =>
            {
                var planned = g.Sum(p => p.PlannedQuantity);
                var actual = actuals
                    .Where(a => a.Date.Date == g.Key.Date && a.ShiftId == g.First().ShiftId)
                    .Sum(a => a.ActualQuantity);
                return new ShiftEfficiencyDto
                {
                    ShiftName = g.Key.Name, Date = g.Key.Date,
                    PlannedQty = planned, ActualQty = actual
                };
            }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<AttendanceHeatmapDto>> GetAttendanceHeatmap(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        return await _db.AttendanceLogs
            .Where(a => a.TenantId == tenantId && a.CheckIn >= from)
            .Include(a => a.User)
            .Select(a => new AttendanceHeatmapDto
            {
                WorkerName = a.User.FullName, Date = a.CheckIn.Date,
                HoursWorked = a.CheckOut.HasValue
                    ? (int)(a.CheckOut.Value - a.CheckIn).TotalHours : 0,
                WasPresent = true
            }).ToListAsync();
    }

    public async Task<List<ProductDistributionDto>> GetProductDistribution(int tenantId)
    {
        var stocks = await _db.WarehouseStocks
            .Where(s => s.TenantId == tenantId)
            .Include(s => s.Product)
            .GroupBy(s => new { s.Product.Name, s.Product.Type })
            .Select(g => new { g.Key.Name, g.Key.Type, Total = (decimal)g.Sum(s => (double)s.Quantity) })
            .ToListAsync();

        var grandTotal = stocks.Sum(s => s.Total);
        return stocks.Select(s => new ProductDistributionDto
        {
            ProductName = s.Name, Type = s.Type, TotalStock = s.Total,
            Percentage = grandTotal > 0 ? Math.Round(s.Total / grandTotal * 100, 1) : 0
        }).ToList();
    }
}
