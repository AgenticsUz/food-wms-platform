using ClosedXML.Excel;
using QuestPDF.Infrastructure;
using WMS.Application.DTOs.Analytics;
using WMS.Application.DTOs.Portal;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Services;
using WMS.Infrastructure.Services.Trade;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Reports;

/// <summary>
/// Hisobot, eksport va kabinet BITTA sanaga — <c>Transfer.DocumentDate</c> ga — qarashini
/// tekshiradi (P2.3), va hujjat qisqa raqami bilan ko'rinishini (P2.4).
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak. Ilgari bir xil davr uchun UCH XIL sana ishlatilardi: analitika
/// <c>ConfirmedAt</c>, Excel eksporti <c>CreatedAt</c>, ishlab chiqarish yana <c>CreatedAt</c>
/// bo'yicha. Natija jim buziladi — hech qayerda xato chiqmaydi, faqat raqamlar bir-biriga
/// to'g'ri kelmaydi va buni faqat mijoz sezadi («Excel'da 12 ta, dashboard'da 9 ta»).
/// </para>
/// <para>
/// ⚠️ Testlarda <c>CreatedAt</c> ni QO'LDA qo'yish SHART EMAS: uni <c>StampEntries</c> BUGUN
/// deb yozadi. Aynan shu kerakli holat — hujjat sanasi kecha, yozuv bugun. Agar filtr
/// <c>CreatedAt</c> ga qarasa, quyidagi testlarning hammasi qizil bo'ladi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class ReportDateAlignmentTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public ReportDateAlignmentTests(WmsDatabaseFixture fixture)
    {
        _fixture = fixture;

        // Prod'da Program.cs qo'yadi; test HTTP xostisiz yuradi, PDF esa litsenziyasiz
        // umuman chizilmaydi. Qayta o'rnatish zararsiz (oddiy statik xossa).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Kecha_sanali_bugun_tasdiqlangan_hujjat_kechagi_kunda_korinadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Plombir");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");

        // Kecha ketgan tovar, BUGUN tasdiqlangan yozuv — P2.3 ning butun mazmuni shu.
        await AddTransferAsync(scope.Db, 1, Day(-1), client.Id, product.Id, 5m, 100m,
            confirmedAt: DateTime.UtcNow);

        IAnalyticsService analytics = scope.Service<IAnalyticsService>();

        List<DailyTransferDto> daily = await analytics.GetDailyTransfers(7);

        DailyTransferDto row = daily.ShouldHaveSingleItem();
        row.Date.ShouldBe(Day(-1));
        row.OutgoingCount.ShouldBe(1);
        row.OutgoingTotal.ShouldBe(500m);

        // Bugungi kunda — HECH NARSA: hujjat bugun tasdiqlangan bo'lsa ham bugungi emas.
        daily.ShouldNotContain(d => d.Date == Day(0));

        List<StockHistoryDto> history = await analytics.GetStockHistory(7);
        history.ShouldHaveSingleItem().Date.ShouldBe(Day(-1));

        // Kengaytirilgan yakun: davr KECHAGI kun bo'lsa hujjat tushadi…
        ExtendedDashboardSummaryDto yesterday = await analytics.GetExtendedDashboardSummary(Day(-1), Day(-1));
        yesterday.TotalOutgoingTransfers.ShouldBe(1);
        yesterday.TotalOutgoingAmount.ShouldBe(500m);

        // …davr faqat BUGUN bo'lsa — tushmaydi (ilgari tasdiq lahzasi tufayli tushardi).
        ExtendedDashboardSummaryDto today = await analytics.GetExtendedDashboardSummary(Day(0), Day(0));
        today.TotalOutgoingTransfers.ShouldBe(0);
    }

    [Fact]
    public async Task Excel_eksporti_va_analitika_bir_xil_toplamni_qaytaradi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Sut");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        // Davr: [-5 … -1]. Chegaralar IKKALASI ham kiradi.
        await AddTransferAsync(scope.Db, 1, Day(-5), client.Id, product.Id, 1m, 100m);
        await AddTransferAsync(scope.Db, 2, Day(-3), client.Id, product.Id, 2m, 100m,
            type: TransferType.Incoming);
        await AddTransferAsync(scope.Db, 3, Day(-1), client.Id, product.Id, 3m, 100m);

        // Davrdan tashqarida — ikkala manbada ham CHIQMASLIGI kerak.
        await AddTransferAsync(scope.Db, 4, Day(-9), client.Id, product.Id, 4m, 100m);
        await AddTransferAsync(scope.Db, 5, Day(0), client.Id, product.Id, 5m, 100m);

        DateTime from = Day(-5);
        DateTime to = Day(-1);

        ExtendedDashboardSummaryDto summary = await scope.Service<IAnalyticsService>()
            .GetExtendedDashboardSummary(from, to);
        int analyticsCount = summary.TotalIncomingTransfers + summary.TotalOutgoingTransfers;

        List<int> exported = ExportedNumbers(await Exports(scope).ExportTransfersAsync(from, to));

        analyticsCount.ShouldBe(3);
        exported.Count.ShouldBe(analyticsCount);

        // Tartib — hujjat sanasi bo'yicha kamayish (ro'yxat ekranidagi kabi).
        exported.ShouldBe([3, 2, 1]);
    }

    [Fact]
    public async Task Kabinet_royxati_hujjat_sanasi_boyicha_tartiblanadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile profile = await TestData.AddUserAsync(scope.Db);
        scope.AsUser(profile.Id);

        // Kabinet egasi tokendagi `sub` bo'yicha yechiladi — kontragentni O'SHA `sub` ga
        // bog'lamasak, PortalService (to'g'ri) 403 berardi.
        Guid sub = scope.Service<TestCurrentUser>().Sub!.Value;
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");
        client.IdentitySub = sub;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Product product = await TestData.AddProductAsync(scope.Db, "Qaymoq");

        // Kiritish tartibi ATAYLAB sana tartibiga teskari: `CreatedAt` bo'yicha tartiblangan
        // ro'yxat 1 → 2 → 3 berardi, hujjat sanasi bo'yicha esa 2 → 3 → 1.
        await AddTransferAsync(scope.Db, 1, Day(-5), client.Id, product.Id, 1m, 100m);
        await AddTransferAsync(scope.Db, 2, Day(-1), client.Id, product.Id, 2m, 100m);
        await AddTransferAsync(scope.Db, 3, Day(-3), client.Id, product.Id, 3m, 100m);

        List<PortalTransferDto> list = await scope.Service<IPortalService>()
            .GetTransfersAsync(1, 50, TestContext.Current.CancellationToken);

        list.Select(t => t.Number).ShouldBe([2, 3, 1]);
        list.Select(t => t.DocumentDate).ShouldBe([Day(-1), Day(-3), Day(-5)]);

        // `CreatedAt` DTO'da qoldi (mijoz «qachon rasmiylashtirildi» ni ham so'raydi), lekin
        // u BUGUN — ya'ni tartib unga qaramaganini ham shu isbotlaydi.
        list[0].CreatedAt.Date.ShouldBe(Day(0));
    }

    [Fact]
    public async Task Eksport_va_pdf_qisqa_raqam_bilan_hujjat_sanasini_korsatadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Tvorog");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");
        Transfer transfer = await AddTransferAsync(scope.Db, 42, Day(-2), client.Id, product.Id, 2m, 250m);

        byte[] bytes = await Exports(scope).ExportTransfersAsync(Day(-7), Day(0));

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet("Transfers");

        // Birinchi ustun — ODAM o'qiydigan raqam; Guid ikkinchi ustunda QOLDI (texnik izlash).
        sheet.Cell(4, 1).GetString().ShouldBe("No");
        sheet.Cell(4, 2).GetString().ShouldBe("ID");
        sheet.Cell(5, 1).GetValue<int>().ShouldBe(42);
        sheet.Cell(5, 2).GetString().ShouldBe(transfer.Id.ToString());

        // «Date» ustuni — hujjat sanasi, yozuv lahzasi emas.
        sheet.Cell(4, 9).GetString().ShouldBe("Date");
        sheet.Cell(5, 9).GetString().ShouldBe(Day(-2).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

        // PDF: chizilishining o'zi tekshiruv — ustunlar soni bilan `ColumnSpan` mos
        // kelmasa QuestPDF chizishda YIQILADI, ya'ni bo'sh bo'lmagan bayt = to'g'ri jadval.
        byte[] pdf = await new TransferPdfService(scope.Db, NoBranding)
            .GenerateTransferPdfAsync(transfer.Id, TestContext.Current.CancellationToken);
        pdf.Length.ShouldBeGreaterThan(0);
    }

    /// <summary>Eksport servisi — DI'dan EMAS, oshkora yasaladi.</summary>
    /// <param name="scope">Tenant qamrovi.</param>
    /// <returns>Servis nusxasi.</returns>
    /// <remarks>
    /// ⚠️ <c>IBrandingFileStore</c> ning prod nusxasi <c>IWebHostEnvironment</c> talab qiladi,
    /// u esa faqat HTTP xostida bor — servis testlari xostsiz yuradi. Brendlash bu testlarning
    /// mavzusi emas (logosiz hisobot ham to'g'ri hisobot), shuning uchun bo'sh do'kon beriladi.
    /// </remarks>
    private static IExportService Exports(WmsTenantScope scope) => new ExportService(scope.Db, NoBranding);

    /// <summary>Logosiz brendlash do'koni.</summary>
    private static IBrandingFileStore NoBranding { get; } = new EmptyBrandingFileStore();

    /// <summary>Hech qanday fayl saqlamaydigan brendlash do'koni (test uchun).</summary>
    private sealed class EmptyBrandingFileStore : IBrandingFileStore
    {
        /// <inheritdoc />
        public Task<string> SaveAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default) =>
            throw new NotSupportedException("Testda brendlash fayli saqlanmaydi.");

        /// <inheritdoc />
        public void Delete(string? publicUrl)
        {
            // Saqlanmagan fayl o'chirilmaydi ham.
        }

        /// <inheritdoc />
        public string? ResolvePath(string? publicUrl) => null;
    }

    /// <summary>UTC kun boshi — hujjat sanasi shu ko'rinishda saqlanadi.</summary>
    /// <param name="daysAgo">Bugundan necha kun oldin (manfiy).</param>
    /// <returns>Sana.</returns>
    private static DateTime Day(int daysAgo) => DateTime.UtcNow.Date.AddDays(daysAgo);

    /// <summary>Eksportdagi hujjatlarning qisqa raqamlari, varaqdagi tartibda.</summary>
    /// <param name="bytes">Excel fayli.</param>
    /// <returns>Raqamlar ro'yxati.</returns>
    /// <remarks>
    /// «Jami» qatorida birinchi katak bo'sh — shuning uchun sanoq raqamli kataklar bo'yicha,
    /// oxirgi qatorni ayirish orqali emas (u jadval o'zgarsa jimgina noto'g'ri bo'lardi).
    /// </remarks>
    private static List<int> ExportedNumbers(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet("Transfers");

        List<int> numbers = [];
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 4;
        for (int row = 5; row <= lastRow; row++)
        {
            if (!sheet.Cell(row, 1).IsEmpty() && sheet.Cell(row, 1).TryGetValue(out int number))
            {
                numbers.Add(number);
            }
        }

        return numbers;
    }

    /// <summary>Bitta qatorli TASDIQLANGAN hujjat (hisobot va eksport o'qiydigan holat).</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="number">Qisqa raqam (tenant ichida noyob).</param>
    /// <param name="documentDate">Hujjat sanasi.</param>
    /// <param name="counterpartyId">Kontragent.</param>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="quantity">Miqdor.</param>
    /// <param name="unitPrice">Narx.</param>
    /// <param name="type">Turi.</param>
    /// <param name="confirmedAt">Tasdiq lahzasi; berilmasa — hozir.</param>
    /// <returns>Yaratilgan hujjat.</returns>
    private static async Task<Transfer> AddTransferAsync(
        WmsDbContext db,
        int number,
        DateTime documentDate,
        Guid counterpartyId,
        Guid productId,
        decimal quantity,
        decimal unitPrice,
        TransferType type = TransferType.Outgoing,
        DateTime? confirmedAt = null)
    {
        Transfer transfer = new()
        {
            Type = type,
            Status = TransferStatus.Confirmed,
            Number = number,
            DocumentDate = documentDate,
            CounterpartyId = counterpartyId,
            ConfirmedAt = confirmedAt ?? DateTime.UtcNow,
        };

        transfer.Items.Add(new TransferItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
        });

        db.Transfers.Add(transfer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return transfer;
    }
}
