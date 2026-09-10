using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Analytics;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>Dashboard va analitika grafiklari.</summary>
/// <remarks>
/// <para>
/// F6: SQLite davrida pul va miqdor <c>double</c> edi va yig'indilar XOTIRADA hisoblanardi
/// (SQLite <c>decimal</c> ni yig'a olmasdi). Endi ustunlar <c>numeric</c> (D13) — yig'indi,
/// sanoq va guruhlash SQL'da, natija shakli o'zgarmadi.
/// </para>
/// <para>
/// ⚠️ Kun/oy chegarasi — <b>UTC</b> (<c>DateTime.Date</c> → <c>date_trunc('day', x, 'UTC')</c>,
/// <c>.Year/.Month</c> → UTC bo'yicha). SQLite davrida ham sana UTC saqlanib, <c>.Date</c> UTC
/// qiymatda olinardi — grafiklar ko'chishda siljimasin. Toshkent (UTC+5) kechasi 00:00–05:00
/// dagi amal oldingi kunga tushadi; tenant vaqt zonasi kerak bo'lsa — alohida qaror.
/// </para>
/// </remarks>
public class AnalyticsService : IAnalyticsService
{
    private readonly WmsDbContext _db;
    public AnalyticsService(WmsDbContext db) => _db = db;

    private static DateTime MonthStart(DateTime utc) => new(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

    // So'rov satridagi sana (`?fromDate=2026-09-01`) Unspecified bo'lib bog'lanadi; ustun
    // konvertori uni UTC deb qabul qiladi (UtcDateTimeConverter) — bu yerda ham xuddi shunday,
    // aks holda standart qiymat bilan aralashgan oraliq ikki xil talqin olardi.
    private static DateTime? AsUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } v => v,
        { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
        { } v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
    };

    public async Task<DashboardSummaryDto> GetDashboardSummary()
    {
        var now = DateTime.UtcNow;
        var monthStart = MonthStart(now);
        var lastMonthStart = monthStart.AddMonths(-1);

        var totalStock = await _db.WarehouseStocks.SumAsync(s => s.Quantity);
        var activeOrders = await _db.ProductionOrders.CountAsync(o => o.Status == ProductionOrderStatus.InProgress);
        var pendingTransfers = await _db.Transfers.CountAsync(t => t.Status == TransferStatus.Pending);
        var monthRevenue = await _db.Transactions
            .Where(t => t.Type == TransactionType.Income && t.Date >= monthStart)
            .SumAsync(t => t.Amount);
        var lastMonthRevenue = await _db.Transactions
            .Where(t => t.Type == TransactionType.Income && t.Date >= lastMonthStart && t.Date < monthStart)
            .SumAsync(t => t.Amount);
        var revenueChange = lastMonthRevenue > 0
            ? Math.Round((monthRevenue - lastMonthRevenue) / lastMonthRevenue * 100, 1) : 0;
        var totalDebt = await _db.Debts.SumAsync(d => d.Amount);

        var lowStockCount = await LowStockCountAsync();

        // Oxirgi 7 kun samaradorligi
        var from7 = now.AddDays(-7);
        var plans = await _db.ShiftPlans.Where(p => p.Date >= from7).SumAsync(p => p.PlannedQuantity);
        var actuals = await _db.ShiftActuals.Where(a => a.Date >= from7).SumAsync(a => a.ActualQuantity);

        return new DashboardSummaryDto
        {
            TotalStockKg = totalStock, ActiveProductionOrders = activeOrders,
            PendingTransfers = pendingTransfers, MonthlyRevenue = monthRevenue,
            RevenueChangePercent = revenueChange, LowStockProductCount = lowStockCount,
            OverallEfficiencyPercent = plans > 0 ? Math.Round(actuals / plans * 100, 1) : 0,
            TotalDebt = totalDebt
        };
    }

    /// <summary>Minimal zaxirasi belgilangan va umumiy qoldig'i undan oshmagan mahsulotlar soni.</summary>
    /// <remarks>Qoldig'i umuman yo'q mahsulot ham «kam» — shuning uchun <c>COALESCE(..., 0)</c>.</remarks>
    private Task<int> LowStockCountAsync() =>
        _db.Products
            .Where(p => p.MinStock > 0)
            .CountAsync(p => (_db.WarehouseStocks
                .Where(s => s.ProductId == p.Id)
                .Sum(s => (decimal?)s.Quantity) ?? 0m) <= p.MinStock);

    public async Task<List<PlanVsActualDto>> GetProductionPlanVsActual(int days)
    {
        var from = DaysAgo(days);
        var planned = await _db.ShiftPlans
            .Where(p => p.Date >= from)
            .GroupBy(p => new { Day = p.Date.Date, p.Product.Name })
            .Select(g => new { g.Key.Day, g.Key.Name, Qty = g.Sum(p => p.PlannedQuantity) })
            .ToListAsync();
        var actual = await _db.ShiftActuals
            .Where(a => a.Date >= from)
            .GroupBy(a => new { Day = a.Date.Date, a.Product.Name })
            .Select(g => new { g.Key.Day, g.Key.Name, Qty = g.Sum(a => a.ActualQuantity) })
            .ToListAsync();
        var actualByKey = actual.ToDictionary(a => (a.Day, a.Name), a => a.Qty);

        return planned
            .Select(p => new PlanVsActualDto
            {
                Date = p.Day, ProductName = p.Name,
                Planned = p.Qty,
                Actual = actualByKey.GetValueOrDefault((p.Day, p.Name))
            }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<WasteByStageDto>> GetWasteByStage(int days)
    {
        var from = DaysAgo(days);
        return await _db.StageExecutions
            .Where(se => se.Status == StageExecutionStatus.Completed && se.EndTime >= from)
            .GroupBy(se => se.RecipeStage.Stage.Name)
            .Select(g => new WasteByStageDto
            {
                StageName = g.Key,
                TotalActual = g.Sum(se => se.ActualQuantity),
                TotalWaste = g.Sum(se => se.WasteQuantity)
            }).ToListAsync();
    }

    public async Task<List<DailyTransferDto>> GetDailyTransfers(int days)
    {
        var from = DaysAgo(days);

        // Sanoq HAMMA tasdiqlangan transferlar bo'yicha guruhlanadi (ichki transfer ham) — eski
        // natijada faqat ichki transferi bo'lgan kun ham nol qator bilan chiqardi.
        var counts = await _db.Transfers
            .Where(t => t.Status == TransferStatus.Confirmed && t.ConfirmedAt >= from)
            .GroupBy(t => new { Day = t.ConfirmedAt!.Value.Date, t.Type })
            .Select(g => new { g.Key.Day, g.Key.Type, Count = g.Count() })
            .ToListAsync();
        var amounts = await _db.TransferItems
            .Where(i => i.Transfer.Status == TransferStatus.Confirmed && i.Transfer.ConfirmedAt >= from
                && (i.Transfer.Type == TransferType.Incoming || i.Transfer.Type == TransferType.Outgoing))
            .GroupBy(i => new { Day = i.Transfer.ConfirmedAt!.Value.Date, i.Transfer.Type })
            .Select(g => new { g.Key.Day, g.Key.Type, Total = g.Sum(i => i.Quantity * i.UnitPrice) })
            .ToListAsync();

        return counts.GroupBy(c => c.Day)
            .Select(g => new DailyTransferDto
            {
                Date = g.Key,
                IncomingTotal = amounts.Where(a => a.Day == g.Key && a.Type == TransferType.Incoming).Sum(a => a.Total),
                OutgoingTotal = amounts.Where(a => a.Day == g.Key && a.Type == TransferType.Outgoing).Sum(a => a.Total),
                IncomingCount = g.Where(c => c.Type == TransferType.Incoming).Sum(c => c.Count),
                OutgoingCount = g.Where(c => c.Type == TransferType.Outgoing).Sum(c => c.Count)
            }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<StockLevelDto>> GetStockLevels(Guid? warehouseId)
    {
        var q = _db.WarehouseStocks.AsQueryable();
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);

        return await q
            .GroupBy(s => new { s.ProductId, s.Product.Name, s.Product.Unit.ShortName, s.Product.MinStock })
            .Select(g => new StockLevelDto
            {
                ProductName = g.Key.Name, UnitShortName = g.Key.ShortName,
                CurrentStock = g.Sum(s => s.Quantity), MinStock = g.Key.MinStock
            }).ToListAsync();
    }

    public async Task<List<StockHistoryDto>> GetStockHistory(int days)
    {
        var from = DaysAgo(days);
        return await _db.Transfers
            .Where(t => t.Status == TransferStatus.Confirmed && t.ConfirmedAt >= from
                && t.Type != TransferType.Internal)
            .SelectMany(t => t.Items.Select(i => new StockHistoryDto
            {
                Date = t.ConfirmedAt!.Value,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                MovementType = t.Type == TransferType.Incoming || t.Type == TransferType.ProductionOutput
                    || t.Type == TransferType.Return
                    ? "Incoming" : "Outgoing"
            })).OrderBy(x => x.Date).ToListAsync();
    }

    public async Task<List<IncomeExpenseDto>> GetIncomeExpense(int days)
    {
        var from = DaysAgo(days);
        var rows = await _db.Transactions
            .Where(t => t.Date >= from)
            .GroupBy(t => t.Date.Date)
            .Select(g => new IncomeExpenseDto
            {
                Date = g.Key,
                Income = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                Expense = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m)
            }).ToListAsync();

        return rows.OrderBy(x => x.Date).ToList();
    }

    public async Task<List<TopDebtorDto>> GetTopDebtors(int top)
    {
        return await _db.Debts.Where(d => d.Amount != 0)
            .OrderByDescending(d => Math.Abs(d.Amount))
            .Take(top)
            .Select(d => new TopDebtorDto
            {
                CounterpartyName = d.Counterparty.Name,
                Type = d.Counterparty.Type,
                DebtAmount = d.Amount
            }).ToListAsync();
    }

    public async Task<List<ShiftEfficiencyDto>> GetShiftEfficiency(int days)
    {
        var from = DaysAgo(days);
        var planned = await _db.ShiftPlans
            .Where(p => p.Date >= from)
            .GroupBy(p => new { p.ShiftId, p.Shift.Name, Day = p.Date.Date })
            .Select(g => new { g.Key.ShiftId, g.Key.Name, g.Key.Day, Qty = g.Sum(p => p.PlannedQuantity) })
            .ToListAsync();
        var actual = await _db.ShiftActuals
            .Where(a => a.Date >= from)
            .GroupBy(a => new { a.ShiftId, Day = a.Date.Date })
            .Select(g => new { g.Key.ShiftId, g.Key.Day, Qty = g.Sum(a => a.ActualQuantity) })
            .ToListAsync();
        var actualByKey = actual.ToDictionary(a => (a.ShiftId, a.Day), a => a.Qty);

        return planned
            .Select(p => new ShiftEfficiencyDto
            {
                ShiftName = p.Name, Date = p.Day,
                PlannedQty = p.Qty, ActualQty = actualByKey.GetValueOrDefault((p.ShiftId, p.Day))
            }).OrderBy(x => x.Date).ToList();
    }

    public async Task<List<AttendanceHeatmapDto>> GetAttendanceHeatmap(int days)
    {
        var from = DaysAgo(days);

        // Yig'ish yo'q (bir log — bir katak), shuning uchun soat va kun xotirada: interval
        // arifmetikasini SQL'ga tarjima qildirishdan ko'ra ishonchli va natija bir xil.
        var logs = await _db.AttendanceLogs
            .Where(a => a.CheckIn >= from)
            .Select(a => new { a.User.FullName, a.CheckIn, a.CheckOut })
            .ToListAsync();

        return logs.Select(a => new AttendanceHeatmapDto
        {
            WorkerName = a.FullName, Date = a.CheckIn.Date,
            HoursWorked = a.CheckOut.HasValue ? (int)(a.CheckOut.Value - a.CheckIn).TotalHours : 0,
            WasPresent = true
        }).ToList();
    }

    public async Task<List<ProductDistributionDto>> GetProductDistribution()
    {
        var stocks = await _db.WarehouseStocks
            .GroupBy(s => new { s.Product.Name, s.Product.Type })
            .Select(g => new { g.Key.Name, g.Key.Type, Total = g.Sum(s => s.Quantity) })
            .ToListAsync();

        var grandTotal = stocks.Sum(s => s.Total);
        return stocks.Select(s => new ProductDistributionDto
        {
            ProductName = s.Name, Type = s.Type, TotalStock = s.Total,
            Percentage = grandTotal > 0 ? Math.Round(s.Total / grandTotal * 100, 1) : 0
        }).ToList();
    }

    public async Task<ExtendedDashboardSummaryDto> GetExtendedDashboardSummary(DateTime? fromDate, DateTime? toDate)
    {
        var now = DateTime.UtcNow;
        var from = AsUtc(fromDate) ?? MonthStart(now);
        var to = AsUtc(toDate) ?? now;

        // Moliya
        var transactions = _db.Transactions.Where(t => t.Date >= from && t.Date <= to);
        var totalIncome = await transactions.Where(t => t.Type == TransactionType.Income).SumAsync(t => t.Amount);
        var totalExpense = await transactions.Where(t => t.Type == TransactionType.Expense).SumAsync(t => t.Amount);
        var totalDebt = await _db.Debts.SumAsync(d => d.Amount);

        // Transferlar
        var confirmed = _db.Transfers.Where(t => t.Status == TransferStatus.Confirmed
            && t.ConfirmedAt >= from && t.ConfirmedAt <= to);
        var confirmedItems = _db.TransferItems.Where(i => i.Transfer.Status == TransferStatus.Confirmed
            && i.Transfer.ConfirmedAt >= from && i.Transfer.ConfirmedAt <= to);

        var incomingCount = await confirmed.CountAsync(t => t.Type == TransferType.Incoming);
        var outgoingCount = await confirmed.CountAsync(t => t.Type == TransferType.Outgoing);
        var incomingAmount = await confirmedItems.Where(i => i.Transfer.Type == TransferType.Incoming)
            .SumAsync(i => i.Quantity * i.UnitPrice);
        var outgoingAmount = await confirmedItems.Where(i => i.Transfer.Type == TransferType.Outgoing)
            .SumAsync(i => i.Quantity * i.UnitPrice);

        // Ishlab chiqarish
        var orders = _db.ProductionOrders.Where(o => o.CreatedAt >= from && o.CreatedAt <= to);
        var totalOrders = await orders.CountAsync();
        var completedOrders = await orders.CountAsync(o => o.Status == ProductionOrderStatus.Completed);
        var executions = _db.StageExecutions.Where(se => se.Status == StageExecutionStatus.Completed
            && se.EndTime >= from && se.EndTime <= to);
        var totalProduced = await executions.SumAsync(e => e.ActualQuantity);
        var totalWaste = await executions.SumAsync(e => e.WasteQuantity);

        // Ombor
        var totalStockValue = await _db.WarehouseStocks.SumAsync(s => s.Quantity * (s.Product.CostPrice ?? 0m));
        var lowStockCount = await LowStockCountAsync();

        // Yetakchilar
        var topProductId = await confirmedItems
            .Where(i => i.Transfer.Type == TransferType.Outgoing)
            .GroupBy(i => i.ProductId)
            .OrderByDescending(g => g.Sum(i => i.Quantity))
            .Select(g => (Guid?)g.Key)
            .FirstOrDefaultAsync();
        var topSellingProduct = topProductId is { } productId
            ? await _db.Products.Where(p => p.Id == productId).Select(p => p.Name).FirstOrDefaultAsync()
            : null;

        var topDebtor = await _db.Debts
            .OrderByDescending(d => d.Amount)
            .Select(d => d.Counterparty.Name)
            .FirstOrDefaultAsync();

        var topSupplierId = await confirmed
            .Where(t => t.Type == TransferType.Incoming && t.CounterpartyId != null)
            .GroupBy(t => t.CounterpartyId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();
        var topSupplier = topSupplierId is { } supplierId
            ? await _db.Counterparties.Where(c => c.Id == supplierId).Select(c => c.Name).FirstOrDefaultAsync()
            : null;

        return new ExtendedDashboardSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetProfit = totalIncome - totalExpense,
            TotalDebt = totalDebt,
            TotalIncomingTransfers = incomingCount,
            TotalOutgoingTransfers = outgoingCount,
            TotalIncomingAmount = incomingAmount,
            TotalOutgoingAmount = outgoingAmount,
            TotalProductionOrders = totalOrders,
            CompletedOrders = completedOrders,
            TotalProduced = totalProduced,
            TotalWaste = totalWaste,
            TotalStockValue = totalStockValue,
            LowStockCount = lowStockCount,
            TopSellingProduct = topSellingProduct,
            TopDebtor = topDebtor,
            TopSupplier = topSupplier
        };
    }

    public async Task<List<MonthlyComparisonDto>> GetMonthlyComparison()
    {
        var currentMonth = MonthStart(DateTime.UtcNow);
        var from = currentMonth.AddMonths(-5);

        var sums = await _db.Transactions
            .Where(t => t.Date >= from)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.Year, g.Key.Month,
                Income = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                Expense = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m)
            }).ToListAsync();

        // Oylar ro'yxati kodda: amal bo'lmagan oy ham grafikda nol bilan tursin.
        var result = new List<MonthlyComparisonDto>();
        for (int i = -5; i <= 0; i++)
        {
            var month = currentMonth.AddMonths(i);
            var row = sums.FirstOrDefault(s => s.Year == month.Year && s.Month == month.Month);
            result.Add(new MonthlyComparisonDto
            {
                Month = month.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                Income = row?.Income ?? 0,
                Expense = row?.Expense ?? 0
            });
        }
        return result;
    }
}
