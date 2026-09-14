using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Import;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>Excel'dan ommaviy import (mahsulot, kontragent) va bo'sh shablonlar.</summary>
/// <remarks>
/// F6: foydalanuvchi importi O'CHDI (D7) — foydalanuvchi Identity'da, Console orqali yaratiladi;
/// WMS'da parol ham yo'q. Shu bilan importdagi plan limiti (<c>MaxUsers</c>) tekshiruvi ham
/// ketdi — qolgan importlar limitli resursga tegmaydi. <c>TenantId</c> qo'yilmaydi:
/// <c>StampEntries</c> joriy tenantni o'zi yozadi (D4).
/// </remarks>
public class ImportService : IImportService
{
    private readonly WmsDbContext _db;
    public ImportService(WmsDbContext db) => _db = db;

    // ────────────────────────────── IMPORT PRODUCTS ──────────────────────────────

    public async Task<ImportResultDto> ImportProductsAsync(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        var result = new ImportResultDto();

        var categories = await _db.Categories.ToListAsync();
        var units = await _db.Units.ToListAsync();

        for (int row = 2; row <= lastRow; row++)
        {
            var name = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) continue;

            result.TotalRows++;
            var errors = new List<ImportErrorDto>();

            // Name (majburiy)
            if (string.IsNullOrWhiteSpace(name))
                errors.Add(new ImportErrorDto { Row = row, Field = "Name", Message = "Name is required" });

            // Category (majburiy, nom bo'yicha)
            var categoryName = ws.Cell(row, 2).GetString().Trim();
            var category = categories.FirstOrDefault(c =>
                c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(categoryName))
                errors.Add(new ImportErrorDto { Row = row, Field = "Category", Message = "Category is required" });
            else if (category == null)
                errors.Add(new ImportErrorDto { Row = row, Field = "Category", Message = $"Category '{categoryName}' not found" });

            // Type (majburiy, katta-kichik harf farqsiz)
            var typeStr = ws.Cell(row, 3).GetString().Trim();
            if (!TryParseProductType(typeStr, out var productType))
                errors.Add(new ImportErrorDto { Row = row, Field = "Type", Message = $"Invalid type '{typeStr}'. Use: Raw, SemiFinished, Finished" });

            // Unit (majburiy, nom yoki qisqa nom bo'yicha)
            var unitName = ws.Cell(row, 4).GetString().Trim();
            var unit = units.FirstOrDefault(u =>
                u.Name.Equals(unitName, StringComparison.OrdinalIgnoreCase) ||
                u.ShortName.Equals(unitName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(unitName))
                errors.Add(new ImportErrorDto { Row = row, Field = "Unit", Message = "Unit is required" });
            else if (unit == null)
                errors.Add(new ImportErrorDto { Row = row, Field = "Unit", Message = $"Unit '{unitName}' not found" });

            // MinStock
            decimal minStock = 0;
            var minStockStr = ws.Cell(row, 5).GetString().Trim();
            if (!string.IsNullOrEmpty(minStockStr) &&
                !decimal.TryParse(minStockStr, NumberStyles.Number, CultureInfo.InvariantCulture, out minStock))
                errors.Add(new ImportErrorDto { Row = row, Field = "MinStock", Message = "Must be a number" });

            // CostPrice
            decimal? costPrice = null;
            var costPriceStr = ws.Cell(row, 6).GetString().Trim();
            if (!string.IsNullOrEmpty(costPriceStr))
            {
                if (decimal.TryParse(costPriceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var cp)) costPrice = cp;
                else errors.Add(new ImportErrorDto { Row = row, Field = "CostPrice", Message = "Must be a number" });
            }

            // ShelfLifeDays
            int? shelfLife = null;
            var shelfStr = ws.Cell(row, 7).GetString().Trim();
            if (!string.IsNullOrEmpty(shelfStr))
            {
                if (int.TryParse(shelfStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sl)) shelfLife = sl;
                else errors.Add(new ImportErrorDto { Row = row, Field = "ShelfLifeDays", Message = "Must be an integer" });
            }

            var barcode = ws.Cell(row, 8).GetString().Trim();

            if (errors.Count > 0)
            {
                result.Errors.AddRange(errors);
                result.ErrorCount++;
                continue;
            }

            try
            {
                _db.Products.Add(new Product
                {
                    Name = name,
                    // Qidiruv ustuni nom bilan BIRGA (P2.1): importdan kelgan mahsulot ham
                    // birinchi kunidan qidiruvda topilsin.
                    NameSearch = SearchNormalizer.Normalize(name),
                    CategoryId = category!.Id,
                    Type = productType,
                    UnitId = unit!.Id,
                    MinStock = minStock,
                    CostPrice = costPrice,
                    ShelfLifeDays = shelfLife,
                    Barcode = string.IsNullOrEmpty(barcode) ? null : barcode
                });
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportErrorDto { Row = row, Field = "-", Message = ex.Message });
                result.ErrorCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportErrorDto { Row = 0, Field = "-", Message = $"Failed to save imported products: {ex.Message}" });
                result.ErrorCount += result.SuccessCount;
                result.SuccessCount = 0;
            }
        }

        return result;
    }

    // ────────────────────────────── IMPORT COUNTERPARTIES ──────────────────────────────

    public async Task<ImportResultDto> ImportCounterpartiesAsync(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        var result = new ImportResultDto();

        for (int row = 2; row <= lastRow; row++)
        {
            var name = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) continue;

            result.TotalRows++;
            var errors = new List<ImportErrorDto>();

            // Name (majburiy)
            if (string.IsNullOrWhiteSpace(name))
                errors.Add(new ImportErrorDto { Row = row, Field = "Name", Message = "Name is required" });

            // Type (majburiy, katta-kichik harf farqsiz)
            var typeStr = ws.Cell(row, 2).GetString().Trim();
            if (!TryParseCounterpartyType(typeStr, out var cpType))
                errors.Add(new ImportErrorDto { Row = row, Field = "Type", Message = $"Invalid type '{typeStr}'. Use: Supplier, Client, Both" });

            var phone = ws.Cell(row, 3).GetString().Trim();
            var address = ws.Cell(row, 4).GetString().Trim();
            var note = ws.Cell(row, 5).GetString().Trim();
            var inn = ws.Cell(row, 6).GetString().Trim();   // ixtiyoriy STIR ustuni

            if (errors.Count > 0)
            {
                result.Errors.AddRange(errors);
                result.ErrorCount++;
                continue;
            }

            // F6: global INN katalogi (Organization) va uning matcher'i O'CHDI (D10) —
            // RLS ostida global jadval tenantlar orasida ma'lumot oqizardi. STIR kontragentning
            // o'zida saqlanadi.
            _db.Counterparties.Add(new Counterparty
            {
                Name = name,
                // Qidiruv ustuni nom bilan BIRGA (P2.1).
                NameSearch = SearchNormalizer.Normalize(name),
                Type = cpType,
                Phone = string.IsNullOrEmpty(phone) ? null : phone,
                Address = string.IsNullOrEmpty(address) ? null : address,
                Note = string.IsNullOrEmpty(note) ? null : note,
                Inn = string.IsNullOrEmpty(inn) ? null : inn
            });
            result.SuccessCount++;
        }

        if (result.SuccessCount > 0)
            await _db.SaveChangesAsync();

        return result;
    }

    // ────────────────────────────── TEMPLATES ──────────────────────────────

    public byte[] GenerateProductsTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        // Sarlavha qatori
        ws.Range("A1:H1").Merge();
        var titleCell = ws.Cell("A1");
        titleCell.Value = "Products Import Template";
        StyleTitle(titleCell);

        // Ustun nomlari
        var headers = new[] { "Name*", "Category", "Type*", "Unit", "MinStock", "CostPrice", "ShelfLifeDays", "Barcode" };
        for (int i = 0; i < headers.Length; i++)
            StyleHeader(ws.Cell(2, i + 1), headers[i]);

        // Namuna qatorlar
        var examples = new[]
        {
            new[] { "Quruq sut", "Sut mahsulotlari", "Raw", "kg", "100", "25000", "365", "" },
            new[] { "Shakar", "Qand va shakar", "Raw", "kg", "200", "12000", "", "" },
            new[] { "Plombir 100ml", "Muzqaymoq", "Finished", "dona", "500", "3500", "180", "" }
        };
        for (int r = 0; r < examples.Length; r++)
            for (int c = 0; c < examples[r].Length; c++)
                StyleExample(ws.Cell(r + 3, c + 1), examples[r][c]);

        ws.Cell("A3").GetComment().AddText("* Required fields. Delete example rows before importing.");

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public byte[] GenerateCounterpartiesTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Counterparties");

        // Sarlavha qatori
        ws.Range("A1:F1").Merge();
        var titleCell = ws.Cell("A1");
        titleCell.Value = "Counterparties Import Template";
        StyleTitle(titleCell);

        // INN (STIR) ixtiyoriy: kontragent kartasida saqlanadi va hisob-faktura uchun kerak.
        var headers = new[] { "Name*", "Type*", "Phone", "Address", "Note", "INN" };
        for (int i = 0; i < headers.Length; i++)
            StyleHeader(ws.Cell(2, i + 1), headers[i]);

        // Namuna qatorlar
        var examples = new[]
        {
            new[] { "Nemat Agro", "Supplier", "998901111111", "Toshkent", "", "123456789" },
            new[] { "Korzinka", "Client", "998902222222", "Toshkent", "Katta mijoz", "" }
        };
        for (int r = 0; r < examples.Length; r++)
            for (int c = 0; c < examples[r].Length; c++)
                StyleExample(ws.Cell(r + 3, c + 1), examples[r][c]);

        ws.Cell("A3").GetComment().AddText("* Required fields. Delete example rows before importing.");

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    // ────────────────────────────── HELPERS ──────────────────────────────

    private static void StyleTitle(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 14;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6366f1");
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.WorksheetRow().Height = 36;
    }

    private static void StyleHeader(IXLCell cell, string value)
    {
        cell.Value = value;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.FromHtml("#3730a3");
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e0e7ff");
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#6366f1");
    }

    private static void StyleExample(IXLCell cell, string value)
    {
        cell.Value = value;
        cell.Style.Font.Italic = true;
        cell.Style.Font.FontColor = XLColor.FromHtml("#64748b");
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
    }

    private static byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static bool TryParseProductType(string value, out ProductType result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Enum.TryParse(value.Replace(" ", "", StringComparison.Ordinal), true, out result);
    }

    private static bool TryParseCounterpartyType(string value, out CounterpartyType result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Enum.TryParse(value.Replace(" ", "", StringComparison.Ordinal), true, out result);
    }
}
