using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class TransferPdfService : ITransferPdfService
{
    private readonly WmsDbContext _db;
    public TransferPdfService(WmsDbContext db) => _db = db;

    private static readonly string Indigo = "#6366f1";
    private static readonly string IndigoLight = "#e0e7ff";
    private static readonly string TextMuted = "#64748b";
    private static readonly string AltRow = "#f8fafc";

    public async Task<byte[]> GenerateTransferPdfAsync(int transferId, int tenantId)
    {
        var transfer = await _db.Transfers
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .Include(t => t.Items).ThenInclude(i => i.Batch)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.TenantId == tenantId)
            ?? throw new Exception("Transfer not found");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, transfer));
                page.Content().Element(c => ComposeContent(c, transfer));
                page.Footer().Element(ComposeFooter);
            });
        });

        return doc.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Transfer transfer)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("WMS Platform").Bold().FontSize(18).FontColor(Indigo);
                    left.Item().Text("Transfer Document").FontSize(12).FontColor(TextMuted);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text($"Transfer #{transfer.Id}").Bold().FontSize(14);
                    right.Item().AlignRight().Text($"Date: {transfer.CreatedAt:yyyy-MM-dd HH:mm}").FontSize(10).FontColor(TextMuted);
                    right.Item().AlignRight().PaddingTop(4).Element(c => StatusBadge(c, transfer.Status.ToString()));
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Indigo);
        });
    }

    private static void StatusBadge(IContainer container, string status)
    {
        var (bg, fg) = status switch
        {
            "Confirmed" => ("#d1fae5", "#065f46"),
            "Rejected" => ("#fee2e2", "#991b1b"),
            "Cancelled" => ("#f1f5f9", "#475569"),
            _ => ("#fef3c7", "#92400e") // Pending
        };

        container
            .Background(bg)
            .Padding(4)
            .Text(status).FontSize(9).Bold().FontColor(fg);
    }

    private static void ComposeContent(IContainer container, Transfer transfer)
    {
        container.PaddingVertical(5).Column(col =>
        {
            // Info section
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    InfoRow(left, "Counterparty:", transfer.Counterparty?.Name ?? "-");
                    InfoRow(left, "Type:", transfer.Type.ToString());
                    InfoRow(left, "From:", transfer.FromWarehouse?.Name ?? "-");
                    InfoRow(left, "To:", transfer.ToWarehouse?.Name ?? "-");
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(right =>
                {
                    InfoRow(right, "Created by:", transfer.CreatedByUser?.FullName ?? "-");
                    InfoRow(right, "Confirmed at:", transfer.ConfirmedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-");
                    InfoRow(right, "Note:", transfer.Note ?? "-");
                });
            });

            col.Item().PaddingVertical(10).LineHorizontal(0.5f).LineColor(TextMuted);

            // Items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // #
                    columns.RelativeColumn(3);     // Product
                    columns.RelativeColumn(2);     // Lot Number
                    columns.RelativeColumn(1.2f);  // Quantity
                    columns.RelativeColumn(0.8f);  // Unit
                    columns.RelativeColumn(1.2f);  // Unit Price
                    columns.RelativeColumn(1.5f);  // Total
                });

                // Header
                table.Header(header =>
                {
                    HeaderCell(header, "#");
                    HeaderCell(header, "Product");
                    HeaderCell(header, "Lot Number");
                    HeaderCell(header, "Quantity");
                    HeaderCell(header, "Unit");
                    HeaderCell(header, "Unit Price");
                    HeaderCell(header, "Total");
                });

                // Data rows
                var items = transfer.Items.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var bg = i % 2 == 1 ? AltRow : "#ffffff";
                    var total = item.Quantity * item.UnitPrice;

                    DataCell(table, (i + 1).ToString(), bg);
                    DataCell(table, item.Product?.Name ?? "-", bg);
                    DataCell(table, item.Batch?.LotNumber ?? "-", bg);
                    DataCell(table, $"{item.Quantity:N2}", bg, true);
                    DataCell(table, item.Product?.Unit?.ShortName ?? "-", bg);
                    DataCell(table, $"{item.UnitPrice:N2}", bg, true);
                    DataCell(table, $"{total:N2}", bg, true);
                }

                // Total row
                var grandTotal = items.Sum(i => i.Quantity * i.UnitPrice);

                table.Cell().ColumnSpan(6)
                    .Background(IndigoLight).Padding(6)
                    .AlignRight().Text("Total:").Bold().FontSize(10);
                table.Cell()
                    .Background(IndigoLight).Padding(6)
                    .AlignRight().Text($"{grandTotal:N2}").Bold().FontSize(10);
            });

            // Signature section
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Prepared by: _______________").FontSize(10);
                    left.Item().PaddingTop(8).Text("Name: _______________").FontSize(10);
                    left.Item().PaddingTop(8).Text("Date: _______________").FontSize(10);
                });

                row.ConstantItem(40);

                row.RelativeItem().Column(right =>
                {
                    right.Item().Text("Received by: _______________").FontSize(10);
                    right.Item().PaddingTop(8).Text("Name: _______________").FontSize(10);
                    right.Item().PaddingTop(8).Text("Date: _______________").FontSize(10);
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8).FontColor(TextMuted));
            text.Span("WMS Platform - Generated ");
            text.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            text.Span(" - Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            row.ConstantItem(100).Text(label).Bold().FontSize(9).FontColor(TextMuted);
            row.RelativeItem().Text(value).FontSize(9);
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Background(Indigo).Padding(6)
            .Text(text).FontColor("#ffffff").Bold().FontSize(9);
    }

    private static void DataCell(TableDescriptor table, string text, string bg, bool alignRight = false)
    {
        var cell = table.Cell().Background(bg).Padding(5)
            .BorderBottom(0.5f).BorderColor("#e2e8f0");

        if (alignRight)
            cell.AlignRight().Text(text).FontSize(9);
        else
            cell.Text(text).FontSize(9);
    }
}
