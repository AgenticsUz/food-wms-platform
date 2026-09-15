using WMS.Application.DTOs.Analytics;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Reports;

/// <summary>
/// Mahsulot foydasi (P2.5): tushum − SOTILGAN partiyaning tannarxi, va tannarxi
/// NOMA'LUM qatorlarning taqdiri.
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak. Foyda hisoboti — narx qo'yish qarorining asosi. Uni ikki yo'l bilan
/// buzish mumkin va ikkalasi ham JIM buzadi (hisobot chiroyli raqam ko'rsatib turaveradi):
/// </para>
/// <list type="number">
/// <item>Tannarx mahsulot darajasidan olinsa — eski, arzon partiya bugungi qimmat narxda
/// baholanib, foyda kichrayadi (yoki aksincha). Shuning uchun tannarx AYNAN sotilgan
/// partiyadan olingan nusxa (<c>TransferItem.UnitCost</c>).</item>
/// <item><c>UnitCost IS NULL</c> qator tannarxga 0 deb qo'shilsa, o'sha miqdor SOF FOYDA
/// bo'lib ko'rinadi. Aynan shu eng xavfli: raqam katta, sabab ko'rinmaydi. Shuning uchun
/// bunday qatorlar tannarxdan tashqarida, <c>UnknownCostQuantity</c> da e'lon qilinadi.</item>
/// </list>
/// <para>
/// ⚠️ <c>TransferItem.UnitCost</c> testlarda QO'LDA to'ldiriladi: uni chiqim tasdiqlanganda
/// FEFO tanlagan partiyadan yozadigan kod (P2.5 ning <c>TransferService</c> qismi) BOSHQA
/// agentda. Bu yerda tekshirilayotgani — hisobotning o'zi: to'ldirilgan ustunni qanday
/// o'qiydi va bo'shini qanday ko'rsatadi. Birinchi testda tannarx baribir «o'ylab
/// topilmaydi» — FEFO yechuvchi tanlagan partiyaning haqiqiy <c>Batch.UnitCost</c> i
/// ko'chiriladi, ya'ni «ikki partiya turli tannarxda → FEFO birinchisini oladi» zanjiri
/// haqiqiy qoldiq ustida yuradi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class ProductProfitTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public ProductProfitTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ikki_partiya_turli_tannarxda_foyda_FEFO_bolgan_partiyadan_hisoblanadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy");
        Product product = await TestData.AddProductAsync(scope.Db, "Plombir");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");

        // Muddati YAQIN partiya arzon (100), uzoq partiya qimmat (120) — FEFO avval
        // arzonini beradi, ya'ni birinchi sotuvning foydasi kattaroq bo'lishi SHART.
        (Batch cheap, _) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 8m, DateTime.UtcNow.AddDays(10));
        (Batch pricey, _) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 10m, DateTime.UtcNow.AddDays(60));

        cheap.UnitCost = 100m;
        pricey.UnitCost = 120m;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IStockAllocator allocator = scope.Service<IStockAllocator>();

        // Birinchi sotuv: 8 dona — arzon partiya TO'LIQ ketadi.
        decimal firstCost = await TakeFefoCostAsync(allocator, scope.Db, product.Id, warehouse.Id, 8m);
        await AddSaleAsync(scope.Db, 1, Day(-3), client.Id, product.Id, 8m, 150m, firstCost);

        // Ikkinchi sotuv: 5 dona — endi faqat qimmat partiya qolgan.
        decimal secondCost = await TakeFefoCostAsync(allocator, scope.Db, product.Id, warehouse.Id, 5m);
        await AddSaleAsync(scope.Db, 2, Day(-1), client.Id, product.Id, 5m, 160m, secondCost);

        firstCost.ShouldBe(100m);
        secondCost.ShouldBe(120m);

        List<ProductProfitDto> report = await scope.Service<IAnalyticsService>()
            .GetProductProfit(Day(-7), Day(0));

        ProductProfitDto row = report.ShouldHaveSingleItem();
        row.ProductId.ShouldBe(product.Id);
        row.Quantity.ShouldBe(13m);
        row.Revenue.ShouldBe(2000m);          // 8 × 150 + 5 × 160
        row.Cost.ShouldBe(1400m);             // 8 × 100 + 5 × 120
        row.Profit.ShouldBe(600m);
        row.Margin.ShouldBe(30m);             // 600 / 2000
        row.UnknownCostQuantity.ShouldBe(0m);
        row.IsCostComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task Tannarxi_nomalum_qator_foydani_shishirmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Shokolad");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        // Tannarxi MA'LUM sotuv: 10 × (150 − 100) = 500 foyda.
        await AddSaleAsync(scope.Db, 1, Day(-3), client.Id, product.Id, 10m, 150m, 100m);

        // Tannarxi NOMA'LUM sotuv (eski qator, backfill qilinmagan partiya): tushumga
        // kiradi, tannarxga KIRMAYDI.
        await AddSaleAsync(scope.Db, 2, Day(-2), client.Id, product.Id, 4m, 150m, unitCost: null);

        ProductProfitDto row = (await scope.Service<IAnalyticsService>()
            .GetProductProfit(Day(-7), Day(0))).ShouldHaveSingleItem();

        row.Quantity.ShouldBe(14m);
        row.Revenue.ShouldBe(2100m);           // 14 × 150 — noma'lum qator ham sotuv

        // Tannarx FAQAT ma'lum qatordan. Agar noma'lum qator 0 tannarx deb qo'shilganda,
        // `Cost` o'zgarmasdi, LEKIN hisobot buni aytmasdi va 600 foyda «aniq» ko'rinardi.
        row.Cost.ShouldBe(1000m);
        row.UnknownCostQuantity.ShouldBe(4m);
        row.IsCostComplete.ShouldBeFalse();

        // Foyda — YUQORI CHEGARA: 4 donaning tannarxi hali ayirilmagan.
        row.Profit.ShouldBe(1100m);
    }

    [Fact]
    public async Task Davr_tur_va_holat_boyicha_faqat_kerakli_hujjatlar_olinadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Sut");
        Product other = await TestData.AddProductAsync(scope.Db, "Qaymoq");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        // Davr ichida, sotuv, tasdiqlangan — YAGONA hisobga olinadigan hujjat.
        await AddSaleAsync(scope.Db, 1, Day(-2), client.Id, product.Id, 10m, 200m, 120m);

        await AddSaleAsync(scope.Db, 2, Day(-20), client.Id, product.Id, 5m, 200m, 120m);      // davrdan tashqarida
        await AddSaleAsync(scope.Db, 3, Day(-2), client.Id, product.Id, 5m, 900m, 120m,        // tasdiqlanmagan
            status: TransferStatus.Pending);
        await AddSaleAsync(scope.Db, 4, Day(-2), client.Id, product.Id, 5m, 50m, null,         // kirim, sotuv emas
            type: TransferType.Incoming);
        await AddSaleAsync(scope.Db, 5, Day(0), client.Id, other.Id, 3m, 300m, 200m);          // boshqa mahsulot

        IAnalyticsService analytics = scope.Service<IAnalyticsService>();

        List<ProductProfitDto> full = await analytics.GetProductProfit(Day(-7), Day(0));
        List<ProductProfitDto> filtered = await analytics.GetProductProfit(Day(-7), Day(0), product.Id);

        // Ikki mahsulot, foyda bo'yicha kamayish tartibida: Sut 800, Qaymoq 300.
        full.Select(r => r.ProductName).ShouldBe(["Sut", "Qaymoq"]);
        full[0].Revenue.ShouldBe(2000m);
        full[0].Profit.ShouldBe(800m);
        full[1].Profit.ShouldBe(300m);

        // Mahsulot filtri — faqat o'sha qator.
        ProductProfitDto only = filtered.ShouldHaveSingleItem();
        only.ProductId.ShouldBe(product.Id);
        only.Quantity.ShouldBe(10m);
    }

    [Fact]
    public async Task Davr_chegaralari_ikki_tomondan_ham_kiradi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Tvorog");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        // Chegara kunlaridagi hujjatlar — hisobotdan TUSHIB QOLMASLIGI kerak: oy hisobotini
        // `from=1-sana&to=oxirgi sana` deb so'rash eng keng tarqalgan hol.
        await AddSaleAsync(scope.Db, 1, Day(-5), client.Id, product.Id, 1m, 100m, 40m);
        await AddSaleAsync(scope.Db, 2, Day(-1), client.Id, product.Id, 1m, 100m, 40m);
        await AddSaleAsync(scope.Db, 3, Day(-6), client.Id, product.Id, 1m, 100m, 40m);

        ProductProfitDto row = (await scope.Service<IAnalyticsService>()
            .GetProductProfit(Day(-5), Day(-1))).ShouldHaveSingleItem();

        row.Quantity.ShouldBe(2m);
        row.Revenue.ShouldBe(200m);
        row.Profit.ShouldBe(120m);
    }

    /// <summary>UTC kun boshi — hujjat sanasi shu ko'rinishda saqlanadi.</summary>
    /// <param name="daysAgo">Bugundan necha kun oldin (manfiy).</param>
    /// <returns>Sana.</returns>
    private static DateTime Day(int daysAgo) => DateTime.UtcNow.Date.AddDays(daysAgo);

    /// <summary>
    /// FEFO bo'yicha yechadi va tanlangan partiyaning tannarxini qaytaradi.
    /// </summary>
    /// <param name="allocator">FEFO yechuvchi.</param>
    /// <param name="db">Kontekst (yechish natijasini yozish uchun).</param>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="warehouseId">Ombor.</param>
    /// <param name="quantity">Miqdor.</param>
    /// <returns>Tanlangan partiyaning bir birlik tannarxi.</returns>
    /// <remarks>
    /// Test ataylab BITTA partiyaga sig'adigan miqdorni yechadi: bir nechta partiyaga
    /// yoyilgan chiqimda «qanday tannarx yoziladi» (o'rtacha? har partiyaga alohida qator?)
    /// — bu <c>TransferService</c> ning qarori va uni BOSHQA agent yozadi. Hisobot esa
    /// tayyor qiymatni o'qiydi, shuning uchun bu yerda savol ochilmaydi.
    /// </remarks>
    private static async Task<decimal> TakeFefoCostAsync(
        IStockAllocator allocator, WmsDbContext db, Guid productId, Guid warehouseId, decimal quantity)
    {
        FefoAllocation allocation = await allocator.DeductFefoAsync(
            productId, quantity, warehouseId, cancellationToken: TestContext.Current.CancellationToken);

        allocation.IsSatisfied.ShouldBeTrue();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return allocation.Lines.Single().Stock.Batch!.UnitCost!.Value;
    }

    /// <summary>Bitta qatorli chiqim hujjati (hisobot O'QIYDIGAN holat).</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="number">Qisqa raqam (tenant ichida noyob).</param>
    /// <param name="documentDate">Hujjat sanasi.</param>
    /// <param name="counterpartyId">Kontragent.</param>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="quantity">Miqdor.</param>
    /// <param name="unitPrice">Sotuv narxi.</param>
    /// <param name="unitCost">Partiya tannarxi nusxasi; <see langword="null"/> — noma'lum.</param>
    /// <param name="status">Holat.</param>
    /// <param name="type">Hujjat turi.</param>
    private static async Task AddSaleAsync(
        WmsDbContext db,
        int number,
        DateTime documentDate,
        Guid counterpartyId,
        Guid productId,
        decimal quantity,
        decimal unitPrice,
        decimal? unitCost,
        TransferStatus status = TransferStatus.Confirmed,
        TransferType type = TransferType.Outgoing)
    {
        Transfer transfer = new()
        {
            Type = type,
            Status = status,
            Number = number,
            DocumentDate = documentDate,
            CounterpartyId = counterpartyId,
            ConfirmedAt = status == TransferStatus.Confirmed ? DateTime.UtcNow : null,
        };

        transfer.Items.Add(new TransferItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            UnitCost = unitCost,
        });

        db.Transfers.Add(transfer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
