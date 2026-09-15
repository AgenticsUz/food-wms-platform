using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>Excel eksportlari (ClosedXML).</summary>
/// <remarks>
/// F6: <c>tenantId</c> parametrlari o'chdi (D4). Qatorlar baribir to'liq o'qiladi (har biri
/// varaqqa yoziladi), shuning uchun jami qatorlari xotirada — endi <c>decimal</c>, ya'ni aniq.
/// Kalit Guid bo'lgani uchun ID ustuni matn.
/// </remarks>
public class ExportService : IExportService
{
    private readonly WmsDbContext _db;
    private readonly IBrandingFileStore _files;
    public ExportService(WmsDbContext db, IBrandingFileStore files)
    { _db = db; _files = files; }

    /// <summary>
    /// Varaqqa mijozning O'Z nomi (va rastr bo'lsa logosi) qo'yiladi. Begona brend bilan kelgan
    /// hisobot — mijozning birinchi shikoyati; umuman chiqmagan hisobot undan ham yomon, shuning
    /// uchun har qadam «iloji bo'lsa».
    /// </summary>
    private async Task BrandAsync(IXLWorksheet ws, string title, int colCount)
    {
        var brand = await ReportBranding.LoadAsync(_db, _files);
        WriteTitle(ws, brand.Name == ReportBranding.DefaultName ? title : $"{brand.Name} — {title}", colCount);

        if (brand.LogoBytes == null) return;
        try
        {
            using var stream = new MemoryStream(brand.LogoBytes);
            ws.AddPicture(stream).MoveTo(ws.Cell(1, colCount)).WithSize(90, 30);
        }
        catch
        {
            // ClosedXML har formatni qabul qilmaydi (masalan WebP). Nom yetarli.
        }
    }

    public async Task<byte[]> ExportTransfersAsync(DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.Transfers
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Items)
            .AsQueryable();

        // ⚠️ Filtr HUJJAT SANASI bo'yicha (P2.3) — `TransferService.GetAllAsync` va
        // `AnalyticsService` bilan AYNAN bir xil ustun. Ilgari eksport `CreatedAt`, analitika
        // esa `ConfirmedAt` bo'yicha filtrlardi: bir xil davr uchun uch xil to'plam chiqardi va
        // «Excel bilan dashboard to'g'ri kelmayapti» degan shikoyat shundan edi.
        if (fromDate.HasValue) q = q.Where(t => t.DocumentDate >= fromDate.Value);
        // `toDate` — wms-web yuboradigan ANIQ lahza (mahalliy kun oxiri, UTC: `localDayRangeToUtc`),
        // ro'yxat endpointlaridagi kabi `<=`. Ilgari UTC kunga yaxlitlanib bir kun qo'shilardi va
        // eksportga keyingi mahalliy kunning 5 soati (Toshkent +5) qo'shilib ketardi (F6 stendi).
        if (toDate.HasValue) q = q.Where(t => t.DocumentDate <= toDate.Value);

        // Tartib ham hujjat sanasi bo'yicha, tenglikda qisqa raqam — ro'yxat ekranidagi
        // tartibning aynan o'zi (`TransferService.GetAllAsync`).
        var data = await q.AsNoTracking()
            .OrderByDescending(t => t.DocumentDate).ThenByDescending(t => t.Number)
            .ToListAsync();

        var filters = BuildFilterText(fromDate, toDate);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Transfers");

        // ⚠️ Birinchi ustun — ODAM o'qiydigan qisqa raqam («No», P2.4). Guid ustuni («ID»)
        // O'CHIRILMADI: qo'llab-quvvatlash xizmati eksportdagi qatorni bazadagi yozuv bilan
        // shu bo'yicha solishtiradi va qisqa raqam tenantlar bo'ylab noyob emas.
        const int colCount = 10;

        await BrandAsync(ws, "Transfers Export", colCount);
        WriteSubtitle(ws, filters, colCount);

        var headers = new[] { "No", "ID", "Type", "Counterparty", "From Warehouse", "To Warehouse",
            "Status", "Total Amount", "Date", "Created By" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var t = data[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = t.Number;
            ws.Cell(row, 2).Value = t.Id.ToString();
            ws.Cell(row, 3).Value = t.Type.ToString();
            ws.Cell(row, 4).Value = t.Counterparty?.Name ?? "-";
            ws.Cell(row, 5).Value = t.FromWarehouse?.Name ?? "-";
            ws.Cell(row, 6).Value = t.ToWarehouse?.Name ?? "-";
            ws.Cell(row, 7).Value = t.Status.ToString();
            ws.Cell(row, 8).Value = t.Items.Sum(x => x.Quantity * x.UnitPrice);
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

            // Sana — HUJJAT sanasi (kun). Soat ataylab yo'q: `DocumentDate` kun boshi bo'lib
            // saqlanadi va «00:00» ustuni o'qiyotgan odamni chalg'itardi.
            ws.Cell(row, 9).Value = t.DocumentDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 10).Value = t.CreatedByUser?.FullName ?? "-";
            StyleDataRow(ws, row, colCount, i % 2 == 1);
        }

        if (data.Count > 0)
        {
            var totalRow = data.Count + 5;
            ws.Cell(totalRow, 7).Value = "Total:";
            ws.Cell(totalRow, 7).Style.Font.Bold = true;
            ws.Cell(totalRow, 8).Value = data.Sum(t => t.Items.Sum(x => x.Quantity * x.UnitPrice));
            ws.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, colCount);
        }

        AutoFit(ws, colCount);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportStockAsync(Guid? warehouseId)
    {
        var q = _db.WarehouseStocks
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .Include(s => s.Product).ThenInclude(p => p.Unit)
            .Include(s => s.Warehouse)
            .Include(s => s.Location)
            .Include(s => s.Batch)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);

        var data = await q.AsNoTracking().OrderBy(s => s.Product.Name).ToListAsync();

        var filterParts = new List<string> { $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" };
        if (warehouseId.HasValue)
        {
            var whName = await _db.Warehouses
                .Where(w => w.Id == warehouseId.Value)
                .Select(w => w.Name)
                .FirstOrDefaultAsync();
            if (whName != null) filterParts.Add($"Warehouse: {whName}");
        }

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Stock");

        await BrandAsync(ws, "Stock Export", 12);
        WriteSubtitle(ws, string.Join(" | ", filterParts), 12);

        var headers = new[] { "Product", "Category", "Type", "Warehouse", "Location",
            "Lot Number", "Quantity", "Unit", "Reserved", "Available", "Expiry Date", "Status" };
        WriteHeaders(ws, 4, headers);

        var now = DateTime.UtcNow;
        for (int i = 0; i < data.Count; i++)
        {
            var s = data[i];
            var row = i + 5;
            var available = s.Quantity - s.ReservedQuantity;
            var isExpired = s.Batch.ExpiryDate.HasValue && s.Batch.ExpiryDate.Value < now;
            var isLow = s.Quantity <= s.Product.MinStock;

            ws.Cell(row, 1).Value = s.Product.Name;
            ws.Cell(row, 2).Value = s.Product.Category?.Name ?? "-";
            ws.Cell(row, 3).Value = s.Product.Type.ToString();
            ws.Cell(row, 4).Value = s.Warehouse.Name;
            ws.Cell(row, 5).Value = s.Location?.Name ?? "-";
            ws.Cell(row, 6).Value = s.Batch.LotNumber;
            ws.Cell(row, 7).Value = s.Quantity;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = s.Product.Unit?.ShortName ?? "-";
            ws.Cell(row, 9).Value = s.ReservedQuantity;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = available;
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
            ws.Cell(totalRow, 7).Value = data.Sum(s => s.Quantity);
            ws.Cell(totalRow, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(totalRow, 9).Value = data.Sum(s => s.ReservedQuantity);
            ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(totalRow, 10).Value = data.Sum(s => s.Quantity - s.ReservedQuantity);
            ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, 12);
        }

        AutoFit(ws, 12);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportTransactionsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.Transactions
            .Include(t => t.Counterparty)
            .Include(t => t.RecordedByUser)
            .AsQueryable();

        if (fromDate.HasValue) q = q.Where(t => t.Date >= fromDate.Value);
        // Aniq lahza, `<=` — sababi ExportTransfersAsync'da.
        if (toDate.HasValue) q = q.Where(t => t.Date <= toDate.Value);

        var data = await q.AsNoTracking().OrderByDescending(t => t.Date).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Transactions");

        await BrandAsync(ws, "Transactions Export", 7);
        WriteSubtitle(ws, BuildFilterText(fromDate, toDate), 7);

        var headers = new[] { "ID", "Type", "Counterparty", "Amount", "Description", "Date", "Recorded By" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var t = data[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = t.Id.ToString();
            ws.Cell(row, 2).Value = t.Type.ToString();
            ws.Cell(row, 3).Value = t.Counterparty?.Name ?? "-";
            ws.Cell(row, 4).Value = t.Amount;
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
            ws.Cell(totalRow, 4).Value = data.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);
            ws.Cell(totalRow, 4).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, 7);
        }

        AutoFit(ws, 7);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportProductsAsync()
    {
        var data = await _db.Products
            .Include(p => p.Category).Include(p => p.Unit)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");

        await BrandAsync(ws, "Products Export", 9);
        WriteSubtitle(ws, $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | Total: {data.Count} products", 9);

        var headers = new[] { "ID", "Name", "Category", "Type", "Unit", "Min Stock",
            "Shelf Life (days)", "Cost Price", "Barcode" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var p = data[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = p.Id.ToString();
            ws.Cell(row, 2).Value = p.Name;
            ws.Cell(row, 3).Value = p.Category?.Name ?? "-";
            ws.Cell(row, 4).Value = p.Type.ToString();
            ws.Cell(row, 5).Value = p.Unit?.ShortName ?? "-";
            ws.Cell(row, 6).Value = p.MinStock;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = p.ShelfLifeDays?.ToString() ?? "N/A";
            ws.Cell(row, 8).Value = p.CostPrice ?? 0m;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = p.Barcode ?? "-";
            StyleDataRow(ws, row, 9, i % 2 == 1);
        }

        AutoFit(ws, 9);
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportCounterpartiesAsync(CounterpartyType? type)
    {
        var q = _db.Counterparties.AsQueryable();
        if (type.HasValue) q = q.Where(c => c.Type == type.Value);

        var data = await q.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        // Debt (tenant_id, counterparty_id) bo'yicha NOYOB (D13) — kontragentga bitta balans.
        var debtByCounterparty = await _db.Debts
            .Select(d => new { d.CounterpartyId, d.Amount })
            .ToDictionaryAsync(d => d.CounterpartyId, d => d.Amount);

        var filterParts = new List<string> { $"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" };
        if (type.HasValue) filterParts.Add($"Type: {type.Value}");
        filterParts.Add($"Total: {data.Count}");

        // F6: «Portal Status» ustuni O'CHDI — kontragent portali birinchi versiyada yo'q (D8).
        const int colCount = 6;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Counterparties");

        await BrandAsync(ws, "Counterparties Export", colCount);
        WriteSubtitle(ws, string.Join(" | ", filterParts), colCount);

        var headers = new[] { "ID", "Name", "Type", "Phone", "Address", "Balance" };
        WriteHeaders(ws, 4, headers);

        for (int i = 0; i < data.Count; i++)
        {
            var c = data[i];
            var row = i + 5;
            var balance = debtByCounterparty.GetValueOrDefault(c.Id);

            ws.Cell(row, 1).Value = c.Id.ToString();
            ws.Cell(row, 2).Value = c.Name;
            ws.Cell(row, 3).Value = c.Type.ToString();
            ws.Cell(row, 4).Value = c.Phone ?? "-";
            ws.Cell(row, 5).Value = c.Address ?? "-";
            ws.Cell(row, 6).Value = balance;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            StyleDataRow(ws, row, colCount, i % 2 == 1);

            if (balance < 0)
                ws.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
        }

        if (data.Count > 0)
        {
            var totalRow = data.Count + 5;
            ws.Cell(totalRow, 5).Value = "Total Balance:";
            ws.Cell(totalRow, 5).Style.Font.Bold = true;
            ws.Cell(totalRow, 6).Value = data.Sum(c => debtByCounterparty.GetValueOrDefault(c.Id));
            ws.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0.00";
            StyleTotalRow(ws, totalRow, colCount);
        }

        AutoFit(ws, colCount);
        return ToBytes(wb);
    }

    // ── Umumiy uslub yordamchilari ──

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
