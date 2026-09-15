using WMS.Application.DTOs.Pricing;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Trade;

/// <summary>
/// «Oxirgi narx» taklifi (P2.2): qaysi hujjatdan olinadi va qaysilari HISOBGA OLINMAYDI.
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak. Taklif formadagi narx maydoniga tushadi, ya'ni ko'p hollarda odam
/// uni o'zgartirmasdan tasdiqlaydi. Noto'g'ri manbadan olingan raqam shu yo'l bilan
/// HUJJATGA aylanadi, keyin esa o'zi keyingi taklifning manbasiga aylanib, xatoni
/// ko'paytiradi. Shuning uchun uchta chegara aynan shu yerda qo'riqlanadi:
/// </para>
/// <list type="number">
/// <item>Kontragent bilan kelishilgan narx umumiy narxdan USTUN (chegirma shartlari
/// har mijozda har xil).</item>
/// <item>Tasdiqlanmagan hujjat — hali sotuv emas, narxi ham hali narx emas.</item>
/// <item>Kirim va sotuv narxi ARALASHMAYDI: sotuv narxi kirimga tushsa, tannarx sotuv
/// darajasiga ko'tarilib foyda nolga tushardi.</item>
/// </list>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class PricingServiceTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public PricingServiceTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Uch_hujjatdan_kontragent_bilan_boglangani_tanlanadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Plombir");
        Counterparty korzinka = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");
        Counterparty makro = await TestData.AddCounterpartyAsync(scope.Db, "Makro");

        // Korzinka bilan ikkita sotuv (eskisi 1000, yangisi 2000), Makro bilan ENG OXIRGI 3000.
        await AddSaleAsync(scope.Db, 1, Day(-10), korzinka.Id, product.Id, 1000m);
        await AddSaleAsync(scope.Db, 2, Day(-5), korzinka.Id, product.Id, 2000m);
        await AddSaleAsync(scope.Db, 3, Day(-2), makro.Id, product.Id, 3000m);

        IPricingService pricing = scope.Service<IPricingService>();

        LastPriceDto? forKorzinka = await pricing.GetLastPriceAsync(
            product.Id, korzinka.Id, TransferType.Outgoing, TestContext.Current.CancellationToken);
        LastPriceDto? general = await pricing.GetLastPriceAsync(
            product.Id, null, TransferType.Outgoing, TestContext.Current.CancellationToken);

        // Korzinka uchun — u bilan bo'lgan OXIRGI narx, umumiy eng yangi 3000 EMAS.
        forKorzinka.ShouldNotBeNull();
        forKorzinka.UnitPrice.ShouldBe(2000m);
        forKorzinka.Number.ShouldBe(2);
        forKorzinka.CounterpartyId.ShouldBe(korzinka.Id);
        forKorzinka.CounterpartyName.ShouldBe("Korzinka");
        forKorzinka.IsSameCounterparty.ShouldBeTrue();
        forKorzinka.DocumentDate.ShouldBe(Day(-5));

        // Kontragentsiz so'rov — umuman oxirgi hujjat.
        general.ShouldNotBeNull();
        general.UnitPrice.ShouldBe(3000m);
        general.CounterpartyName.ShouldBe("Makro");
        general.IsSameCounterparty.ShouldBeFalse();
    }

    [Fact]
    public async Task Yangi_kontragent_uchun_umumiy_narx_taklif_qilinadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Shokolad");
        Counterparty known = await TestData.AddCounterpartyAsync(scope.Db, "Eski mijoz");
        Counterparty fresh = await TestData.AddCounterpartyAsync(scope.Db, "Yangi mijoz");

        await AddSaleAsync(scope.Db, 1, Day(-3), known.Id, product.Id, 1750m);

        LastPriceDto? suggestion = await scope.Service<IPricingService>().GetLastPriceAsync(
            product.Id, fresh.Id, TransferType.Outgoing, TestContext.Current.CancellationToken);

        // Hech qachon savdo qilmagan mijozga ham taklif bo'lishi kerak — lekin bayroq
        // «bu SIZNING narxingiz emas» deb aytib turadi.
        suggestion.ShouldNotBeNull();
        suggestion.UnitPrice.ShouldBe(1750m);
        suggestion.IsSameCounterparty.ShouldBeFalse();
        suggestion.CounterpartyId.ShouldBe(known.Id);
    }

    [Fact]
    public async Task Tasdiqlanmagan_hujjat_hisobga_olinmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Tvorog");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        await AddSaleAsync(scope.Db, 1, Day(-4), client.Id, product.Id, 1200m);

        // Bugungi hujjat ENG yangi, lekin hali tasdiqlanmagan (va narxi xato kiritilgan).
        await AddSaleAsync(scope.Db, 2, Day(0), client.Id, product.Id, 99_000m, TransferStatus.Pending);

        // Rad etilgan hujjat ham manba bo'lolmaydi.
        await AddSaleAsync(scope.Db, 3, Day(0), client.Id, product.Id, 88_000m, TransferStatus.Rejected);

        LastPriceDto? suggestion = await scope.Service<IPricingService>().GetLastPriceAsync(
            product.Id, client.Id, TransferType.Outgoing, TestContext.Current.CancellationToken);

        suggestion.ShouldNotBeNull();
        suggestion.UnitPrice.ShouldBe(1200m);
        suggestion.Number.ShouldBe(1);
    }

    [Fact]
    public async Task Kirim_va_chiqim_narxi_aralashmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Sut");
        Counterparty supplier = await TestData.AddCounterpartyAsync(
            scope.Db, "Ta'minotchi", CounterpartyType.Supplier);

        // Kirim 8000 (tannarx), sotuv 11 000 — sotuv kechroq bo'lgani muhim: tur bo'yicha
        // ajratilmasa, kirim so'rovi 11 000 ni qaytarardi.
        await AddSaleAsync(scope.Db, 1, Day(-6), supplier.Id, product.Id, 8000m, type: TransferType.Incoming);
        await AddSaleAsync(scope.Db, 2, Day(-1), supplier.Id, product.Id, 11_000m);

        IPricingService pricing = scope.Service<IPricingService>();

        LastPriceDto? incoming = await pricing.GetLastPriceAsync(
            product.Id, supplier.Id, TransferType.Incoming, TestContext.Current.CancellationToken);
        LastPriceDto? outgoing = await pricing.GetLastPriceAsync(
            product.Id, supplier.Id, TransferType.Outgoing, TestContext.Current.CancellationToken);

        incoming.ShouldNotBeNull();
        incoming.UnitPrice.ShouldBe(8000m);
        outgoing.ShouldNotBeNull();
        outgoing.UnitPrice.ShouldBe(11_000m);
    }

    [Fact]
    public async Task Mos_hujjat_bolmasa_null_qaytadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product sold = await TestData.AddProductAsync(scope.Db, "Sotilgan");
        Product never = await TestData.AddProductAsync(scope.Db, "Sotilmagan");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        await AddSaleAsync(scope.Db, 1, Day(-1), client.Id, sold.Id, 500m);

        IPricingService pricing = scope.Service<IPricingService>();

        // Hech qachon sotilmagan mahsulot — taklif yo'q.
        (await pricing.GetLastPriceAsync(
            never.Id, null, TransferType.Outgoing, TestContext.Current.CancellationToken)).ShouldBeNull();

        // Sotilgan, lekin KIRIM narxi yo'q — nol taklif qilinmaydi.
        (await pricing.GetLastPriceAsync(
            sold.Id, null, TransferType.Incoming, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Bir_kundagi_ikki_hujjatdan_keyingi_raqamlisi_olinadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product product = await TestData.AddProductAsync(scope.Db, "Qaymoq");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        // `DocumentDate` — KUN boshi, ya'ni bir kundagi hujjatlar sana bo'yicha TENG.
        // Tartib faqat sanaga tayansa, javob tasodifiy bo'lardi.
        await AddSaleAsync(scope.Db, 7, Day(-1), client.Id, product.Id, 4000m);
        await AddSaleAsync(scope.Db, 8, Day(-1), client.Id, product.Id, 4500m);

        LastPriceDto? suggestion = await scope.Service<IPricingService>().GetLastPriceAsync(
            product.Id, client.Id, TransferType.Outgoing, TestContext.Current.CancellationToken);

        suggestion.ShouldNotBeNull();
        suggestion.UnitPrice.ShouldBe(4500m);
        suggestion.Number.ShouldBe(8);
    }

    /// <summary>UTC kun boshi (hujjat sanasi shunday saqlanadi — <c>DocumentDates.Resolve</c>).</summary>
    /// <param name="daysAgo">Bugundan necha kun oldin (manfiy).</param>
    /// <returns>Sana.</returns>
    private static DateTime Day(int daysAgo) => DateTime.UtcNow.Date.AddDays(daysAgo);

    /// <summary>
    /// Bitta qatorli hujjat yozadi — servis O'QIYDIGAN holatning eng qisqa ko'rinishi.
    /// </summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="number">Qisqa raqam (tenant ichida NOYOB — indeks talab qiladi).</param>
    /// <param name="documentDate">Hujjat sanasi.</param>
    /// <param name="counterpartyId">Kontragent.</param>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="unitPrice">Narx.</param>
    /// <param name="status">Holat.</param>
    /// <param name="type">Hujjat turi.</param>
    /// <returns>Yozilgan hujjat.</returns>
    /// <remarks>
    /// ⚠️ Hujjat <c>TransferService</c> orqali EMAS, to'g'ridan-to'g'ri yoziladi: bu yerda
    /// tekshirilayotgan narsa narx TANLASH mantiqi, hujjat yaratish qoidalari emas (ular
    /// <c>TransferServiceTests</c> da). Shu bilan test zaxira, komissiya va ruxsat
    /// talablaridan mustaqil qoladi.
    /// </remarks>
    private static async Task<Transfer> AddSaleAsync(
        WmsDbContext db,
        int number,
        DateTime documentDate,
        Guid counterpartyId,
        Guid productId,
        decimal unitPrice,
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
            Quantity = 1m,
            UnitPrice = unitPrice,
        });

        db.Transfers.Add(transfer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return transfer;
    }
}
