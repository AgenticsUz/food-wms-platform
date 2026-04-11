using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly WmsDbContext _db;
    public ExportService(WmsDbContext db) => _db = db;

    public async Task<byte[]> ExportTransfersAsync(int tenantId, DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.Transfers.Where(t => t.TenantId == tenantId)
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Items)
            .AsQueryable();

        if (fromDate.HasValue) q = q.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(t => t.CreatedAt <= toDate.Value);

        var data = await q.OrderByDescending(t => t.CreatedAt).ToListAsync();

        var filters = BuildFilterText(fromDate, toDate);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Transfers");

        WriteTitle(ws, "Transfers Export", 9);
        WriteSubtitle(ws, filters, 9);

        var headers = new[] { "ID", "Type", "Counterparty", "From Warehouse", "To Warehouse",
            "Status", "Total Amount", "Date", "Created By" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var t = data[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = t.Id;
            ws.Cell(row, 2).Value = t.Type.ToString();
            ws.Cell(row, 3).Value = t.Counterparty?.Name ?? "-";
            ws.Cell(row, 4).Value = t.FromWarehouse?.Name ?? "-";
            ws.Cell(row, 5).Value = t.ToWarehouse?.Name ?? "-";
            ws.Cell(row, 6).Value = t.Status.ToString();
            ws.Cell(row, 7).Value = (double)t.Items.Sum(x => x.Quantity * x.UnitPrice);
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = t.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 9).Value = t.CreatedByUser?.FullName ?? "-";
            StyleDataRow(ws, row, 9, i % 2 == 1);
        }

        if (data.Count > 0)
        {
            var totalRow = data.Count + 5;
            ws.Cell(totalRow, 6).Value = "Total:";
            ws.Cell(totalRow, 6).Style.Font.Bold = true;
            ws.Cell(totalRow, 7).Value = (double)data.Sum(t => t.Items.Sum(x => x.Quantity * x.UnitPrice));
            ws.Cell(totalRow, 7).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, 9);
        }

        AutoFit(ws, 9);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportStockAsync(int tenantId, int? warehouseId)
    {
        var q = _db.WarehouseStocks.Where(s => s.TenantId == tenantId)
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .Include(s => s.Product).ThenInclude(p => p.Unit)
            .Include(s => s.Warehouse)
            .Include(s => s.Location)
            .Include(s => s.Batch)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);

        var data = await q.OrderBy(s => s.Product.Name).ToListAsync();

        var filterParts = new List<string> { $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" };
        if (warehouseId.HasValue)
        {
            var wh = await _db.Warehouses.FindAsync(warehouseId.Value);
            if (wh != null) filterParts.Add($"Warehouse: {wh.Name}");
        }

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Stock");

        WriteTitle(ws, "Stock Export", 12);
        WriteSubtitle(ws, string.Join(" | ", filterParts), 12);

        var headers = new[] { "Product", "Category", "Type", "Warehouse", "Location",
            "Lot Number", "Quantity", "Unit", "Reserved", "Available", "Expiry Date", "Status" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var s = data[i];
            var row = i + 5;
            var available = s.Quantity - s.ReservedQuantity;
            var isExpired = s.Batch.ExpiryDate.HasValue && s.Batch.ExpiryDate.Value < DateTime.UtcNow;
            var isLow = s.Quantity <= s.Product.MinStock;

            ws.Cell(row, 1).Value = s.Product.Name;
            ws.Cell(row, 2).Value = s.Product.Category?.Name ?? "-";
            ws.Cell(row, 3).Value = s.Product.Type.ToString();
            ws.Cell(row, 4).Value = s.Warehouse.Name;
            ws.Cell(row, 5).Value = s.Location?.Name ?? "-";
            ws.Cell(row, 6).Value = s.Batch.LotNumber;
            ws.Cell(row, 7).Value = (double)s.Quantity;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = s.Product.Unit?.ShortName ?? "-";
            ws.Cell(row, 9).Value = (double)s.ReservedQuantity;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = (double)available;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 11).Value = s.Batch.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A";
            ws.Cell(row, 12).Value = isExpired ? "Expired" : isLow ? "Low Stock" : "OK";
            StyleDataRow(ws, row, 12, i % 2 == 1);

            if (isExpired)
                ws.Cell(row, 12).Style.Font.FontColor = XLColor.Red;
            else if (isLow)
                ws.Cell(row, 12).Style.Font.FontColor = XLColor.FromHtml("#92400e");
        }

        if (data.Count > 0)
        {
            var totalRow = data.Count + 5;
            ws.Cell(totalRow, 6).Value = "Total:";
            ws.Cell(totalRow, 6).Style.Font.Bold = true;
            ws.Cell(totalRow, 7).Value = (double)data.Sum(s => s.Quantity);
            ws.Cell(totalRow, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(totalRow, 9).Value = (double)data.Sum(s => s.ReservedQuantity);
            ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(totalRow, 10).Value = (double)data.Sum(s => s.Quantity - s.ReservedQuantity);
            ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, 12);
        }

        AutoFit(ws, 12);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportTransactionsAsync(int tenantId, DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.Transactions.Where(t => t.TenantId == tenantId)
            .Include(t => t.Counterparty)
            .Include(t => t.RecordedByUser)
            .AsQueryable();

        if (fromDate.HasValue) q = q.Where(t => t.Date >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(t => t.Date <= toDate.Value);

        var data = await q.OrderByDescending(t => t.Date).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Transactions");

        WriteTitle(ws, "Transactions Export", 7);
        WriteSubtitle(ws, BuildFilterText(fromDate, toDate), 7);

        var headers = new[] { "ID", "Type", "Counterparty", "Amount", "Description", "Date", "Recorded By" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var t = data[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = t.Id;
            ws.Cell(row, 2).Value = t.Type.ToString();
            ws.Cell(row, 3).Value = t.Counterparty?.Name ?? "-";
            ws.Cell(row, 4).Value = (double)t.Amount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = t.Description ?? "-";
            ws.Cell(row, 6).Value = t.Date.ToString("yyyy-MM-dd");
            ws.Cell(row, 7).Value = t.RecordedByUser?.FullName ?? "-";
            StyleDataRow(ws, row, 7, i % 2 == 1);
        }

        if (data.Count > 0)
        {
            var totalRow = data.Count + 5;
            var income = data.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expense = data.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            ws.Cell(totalRow, 3).Value = $"Income: {income:N2} | Expense: {expense:N2}";
            ws.Cell(totalRow, 3).Style.Font.Bold = true;
            ws.Cell(totalRow, 4).Value = (double)data.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);
            ws.Cell(totalRow, 4).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, 7);
        }

        AutoFit(ws, 7);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportProductsAsync(int tenantId)
    {
        var data = await _db.Products.Where(p => p.TenantId == tenantId)
            .Include(p => p.Category).Include(p => p.Unit)
            .OrderBy(p => p.Name)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");

        WriteTitle(ws, "Products Export", 9);
        WriteSubtitle(ws, $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | Total: {data.Count} products", 9);

        var headers = new[] { "ID", "Name", "Category", "Type", "Unit", "Min Stock",
            "Shelf Life (days)", "Cost Price", "Barcode" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var p = data[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = p.Id;
            ws.Cell(row, 2).Value = p.Name;
            ws.Cell(row, 3).Value = p.Category?.Name ?? "-";
            ws.Cell(row, 4).Value = p.Type.ToString();
            ws.Cell(row, 5).Value = p.Unit?.ShortName ?? "-";
            ws.Cell(row, 6).Value = (double)p.MinStock;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = p.ShelfLifeDays?.ToString() ?? "N/A";
            ws.Cell(row, 8).Value = p.CostPrice.HasValue ? (double)p.CostPrice.Value : 0;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = p.Barcode ?? "-";
            StyleDataRow(ws, row, 9, i % 2 == 1);
        }

        AutoFit(ws, 9);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportCounterpartiesAsync(int tenantId, CounterpartyType? type)
    {
        var q = _db.Counterparties.Where(c => c.TenantId == tenantId).AsQueryable();
        if (type.HasValue) q = q.Where(c => c.Type == type.Value);

        var data = await q.OrderBy(c => c.Name).ToListAsync();

        var debts = await _db.Debts.Where(d => d.TenantId == tenantId).ToListAsync();

        var filterParts = new List<string> { $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" };
        if (type.HasValue) filterParts.Add($"Type: {type.Value}");
        filterParts.Add($"Total: {data.Count}");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Counterparties");

        WriteTitle(ws, "Counterparties Export", 7);
        WriteSubtitle(ws, string.Join(" | ", filterParts), 7);

        var headers = new[] { "ID", "Name", "Type", "Phone", "Address", "Balance", "Portal Status" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var c = data[i];
            var row = i + 5;
            var debt = debts.FirstOrDefault(d => d.CounterpartyId == c.Id);
            var balance = debt?.Amount ?? 0m;

            ws.Cell(row, 1).Value = c.Id;
            ws.Cell(row, 2).Value = c.Name;
            ws.Cell(row, 3).Value = c.Type.ToString();
            ws.Cell(row, 4).Value = c.Phone ?? "-";
            ws.Cell(row, 5).Value = c.Address ?? "-";
            ws.Cell(row, 6).Value = (double)balance;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = c.PortalEnabled ? "Enabled" : "Disabled";
            StyleDataRow(ws, row, 7, i % 2 == 1);

            if (balance < 0)
                ws.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
        }

        if (data.Count > 0)
        {
            var totalRow = data.Count + 5;
            ws.Cell(totalRow, 5).Value = "Total Balance:";
            ws.Cell(totalRow, 5).Style.Font.Bold = true;
            var totalBalance = data.Sum(c => debts.FirstOrDefault(d => d.CounterpartyId == c.Id)?.Amount ?? 0m);
            ws.Cell(totalRow, 6).Value = (double)totalBalance;
            ws.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, 7);
        }

        AutoFit(ws, 7);
        return ToBytes(wb);
    }

    // ── Shared styling helpers ──

    private static void WriteTitle(IXLWorksheet ws, string title, int colCount)
    {
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, colCount).Merge();
        ws.Row(1).Height = 36;
        var titleStyle = ws.Range(1, 1, 1, colCount).Style;
        titleStyle.Font.Bold = true;
        titleStyle.Font.FontSize = 14;
        titleStyle.Font.FontColor = XLColor.White;
        titleStyle.Fill.BackgroundColor = XLColor.FromHtml("#6366f1");
        titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void WriteSubtitle(IXLWorksheet ws, string text, int colCount)
    {
        ws.Cell(2, 1).Value = text;
        ws.Range(2, 1, 2, colCount).Merge();
        var subStyle = ws.Range(2, 1, 2, colCount).Style;
        subStyle.Font.Italic = true;
        subStyle.Font.FontColor = XLColor.FromHtml("#64748b");
        subStyle.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9");
        subStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteHeaders(IXLWorksheet ws, int row, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml("#3730a3");
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e0e7ff");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#c7d2fe");
        }
    }

    private static void StyleDataRow(IXLWorksheet ws, int row, int colCount, bool alternate)
    {
        if (alternate)
        {
            for (int c = 1; c <= colCount; c++)
                ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
        }
    }

    private static void StyleTotalRow(IXLWorksheet ws, int row, int colCount)
    {
        for (int c = 1; c <= colCount; c++)
        {
            ws.Cell(row, c).Style.Font.Bold = true;
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#e0e7ff");
            ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.FromHtml("#c7d2fe");
        }
    }

    private static void AutoFit(IXLWorksheet ws, int colCount)
    {
        for (int c = 1; c <= colCount; c++)
            ws.Column(c).AdjustToContents();
    }

    private static string BuildFilterText(DateTime? from, DateTime? to)
    {
        var parts = new List<string> { $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" };
        if (from.HasValue) parts.Add($"From: {from.Value:yyyy-MM-dd}");
        if (to.HasValue) parts.Add($"To: {to.Value:yyyy-MM-dd}");
        return string.Join(" | ", parts);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
