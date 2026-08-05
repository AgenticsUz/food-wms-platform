using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class DeliveryPdfService : IDeliveryPdfService
{
    private readonly WmsDbContext _db;
    private readonly IBrandingFileStore _files;
    public DeliveryPdfService(WmsDbContext db, IBrandingFileStore files)
    { _db = db; _files = files; }

    private static readonly string Indigo = "#6366f1";
    private static readonly string TextMuted = "#64748b";
    private static readonly string AltRow = "#f8fafc";

    public async Task<byte[]> GenerateWaybillPdfAsync(int deliveryId, int tenantId)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Vehicle).Include(d => d.Driver).Include(d => d.CreatedByUser)
            .Include(d => d.Stops).ThenInclude(s => s.Counterparty)
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.TenantId == tenantId)
            ?? throw new NotFoundException("Delivery not found");

        var brand = await ReportBranding.LoadAsync(_db, _files, tenantId);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, delivery, brand));
                page.Content().Element(c => ComposeContent(c, delivery));
                page.Footer().Element(ComposeFooter);
            });
        });

        return doc.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Delivery delivery, ReportBranding brand)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (brand.LogoBytes != null)
                    row.ConstantItem(110).PaddingRight(12).AlignMiddle().Image(brand.LogoBytes);

                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(brand.Name).Bold().FontSize(18).FontColor(brand.Color ?? Indigo);
                    left.Item().Text("Delivery Waybill").FontSize(12).FontColor(TextMuted);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text($"Delivery #{delivery.Id}").Bold().FontSize(14);
                    right.Item().AlignRight().Text($"Scheduled: {delivery.ScheduledDate:yyyy-MM-dd}").FontSize(10).FontColor(TextMuted);
                    right.Item().AlignRight().PaddingTop(4).Element(c => StatusBadge(c, delivery.Status.ToString()));
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Indigo);
        });
    }

    private static void StatusBadge(IContainer container, string status)
    {
        var (bg, fg) = status switch
        {
            "Completed" => ("#d1fae5", "#065f46"),
            "Cancelled" => ("#fee2e2", "#991b1b"),
            "InProgress" => ("#dbeafe", "#1e40af"),
            _ => ("#fef3c7", "#92400e") // Planned
        };

        container
            .Background(bg)
            .Padding(4)
            .Text(status).FontSize(9).Bold().FontColor(fg);
    }

    private static void ComposeContent(IContainer container, Delivery delivery)
    {
        container.PaddingVertical(5).Column(col =>
        {
            // Info section
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    InfoRow(left, "Vehicle:", delivery.Vehicle?.Name ?? "-");
                    InfoRow(left, "Driver:", delivery.Driver?.FullName ?? "-");
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(right =>
                {
                    InfoRow(right, "Created by:", delivery.CreatedByUser?.FullName ?? "-");
                    InfoRow(right, "Note:", delivery.Note ?? "-");
                });
            });

            col.Item().PaddingVertical(10).LineHorizontal(0.5f).LineColor(TextMuted);

            // Stops table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // Seq
                    columns.RelativeColumn(3);     // Counterparty
                    columns.RelativeColumn(3);     // Address
                    columns.RelativeColumn(1.5f);  // Status
                });

                table.Header(header =>
                {
                    HeaderCell(header, "#");
                    HeaderCell(header, "Counterparty");
                    HeaderCell(header, "Address");
                    HeaderCell(header, "Status");
                });

                var stops = delivery.Stops.OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id).ToList();
                for (int i = 0; i < stops.Count; i++)
                {
                    var stop = stops[i];
                    var bg = i % 2 == 1 ? AltRow : "#ffffff";

                    DataCell(table, stop.SequenceOrder.ToString(), bg);
                    DataCell(table, stop.Counterparty?.Name ?? "-", bg);
                    DataCell(table, stop.Address ?? "-", bg);
                    DataCell(table, stop.Status.ToString(), bg);
                }
            });

            // Signature section
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Driver: _______________").FontSize(10);
                    left.Item().PaddingTop(8).Text("Signature: _______________").FontSize(10);
                    left.Item().PaddingTop(8).Text("Date: _______________").FontSize(10);
                });

                row.ConstantItem(40);

                row.RelativeItem().Column(right =>
                {
                    right.Item().Text("Received by: _______________").FontSize(10);
                    right.Item().PaddingTop(8).Text("Signature: _______________").FontSize(10);
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

    private static void DataCell(TableDescriptor table, string text, string bg)
    {
        table.Cell().Background(bg).Padding(5)
            .BorderBottom(0.5f).BorderColor("#e2e8f0")
            .Text(text).FontSize(9);
    }
}
