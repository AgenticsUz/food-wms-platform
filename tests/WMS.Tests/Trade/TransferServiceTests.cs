using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Trade;

/// <summary>
/// <see cref="ITransferService"/> ning zaxiraga TEGADIGAN yo'llari: chiqim (FEFO),
/// kirim (partiya yasash), ichki ko'chirish, rad etish va parallel tasdiq.
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak (reja §P1). Bu yo'llarning har biri SQLite → Postgres ko'chishida
/// qayta yozilgan va ularning to'g'riligini hech narsa ushlab turmasdi:
/// </para>
/// <list type="bullet">
/// <item>FEFO tartibi endi SQL'da (<c>StockAllocator</c>) — <c>ORDER BY</c> buzilsa
/// omborda muddati o'tayotgan partiya qolib, yangisi sotilib ketardi va buni faqat
/// yaroqlilik muddati chiqqanda sezishardi.</item>
/// <item>Yaratishdagi qoldiq tekshiruvi (<c>EnsureStockAvailableAsync</c>) — olib
/// tashlansa hujjat jimgina yaratilib, xato mijozga aytib bo'lingandan keyin,
/// tasdiqda chiqardi.</item>
/// <item>Parallel tasdiqning <c>xmin</c> qo'riqchisi (D13) — u yo'qolsa ikkala tasdiq
/// ham o'tib, zaxira IKKI marta kamayardi (SQLite davridagi haqiqiy nosozlik).</item>
/// <item>Rad etish — yo'l bo'ylab zaxiraga tegib ketmasligi kerak.</item>
/// </list>
/// <para>
/// Testlar HAQIQIY Postgres'da yuradi: <c>numeric(18,3)</c> yaxlitlashi, <c>xmin</c>
/// va RLS InMemory provayderida yo'q.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class TransferServiceTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public TransferServiceTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    /// <summary>Noyob kodli yangi test tenanti.</summary>
    /// <returns>Tenant.</returns>
    /// <remarks>
    /// ⚠️ Kod OSHKORA beriladi: fixture'ning sukut kodi Guid v7 ning BOSH 10 belgisidan
    /// yasaladi, ular esa vaqt tamg'asi — bir necha test bir vaqtda tenant yaratsa kod
    /// bir xil chiqib, <c>ix_tenant_code</c> noyobligi buzilardi (23505). Tasodifiy v4
    /// bunday to'qnashmaydi.
    /// </remarks>
    private Task<TestTenant> NewTenantAsync() =>
        _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");

    [Fact]
    public async Task Chiqim_tasdiqlansa_FEFO_eng_yaqin_muddatli_partiyani_oladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid transferId;
        Guid productId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Tayyor mahsulot");
            Product product = await TestData.AddProductAsync(scope.Db, "Sut 1 l");
            productId = product.Id;

            // Uzoq muddatli partiya BIRINCHI yaratiladi: uning `Id` si (Guid v7 — vaqt bo'yicha
            // o'sadi) kichik. Shu sabab FEFO tasodifan `Id` bo'yicha tartiblansa ham, muddat
            // bo'yicha tartiblansa ham FARQLI partiya tanlanadi — test ayni shu farqni o'lchaydi.
            await TestData.AddStockAsync(scope.Db, warehouse, product, 10m,
                expiry: new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "LOT-DEK");
            await TestData.AddStockAsync(scope.Db, warehouse, product, 10m,
                expiry: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "LOT-OKT");

            ITransferService transfers = scope.Service<ITransferService>();
            TransferDto created = await transfers.CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = warehouse.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 4m, UnitPrice = 9000m }],
            });

            created.Status.ShouldBe(TransferStatus.Pending);
            transferId = created.Id;

            TransferDto confirmed = await transfers.ConfirmAsync(transferId);
            confirmed.Status.ShouldBe(TransferStatus.Confirmed);
            confirmed.ConfirmedAt.ShouldNotBeNull();
        }

        // Tekshiruv YANGI qamrovda: EF kuzatuvidagi nusxa emas, bazadagi qiymat o'lchansin.
        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        Dictionary<string, decimal> byLot = await verify.Db.WarehouseStocks
            .Where(s => s.ProductId == productId)
            .Select(s => new { s.Batch.LotNumber, s.Quantity })
            .ToDictionaryAsync(x => x.LotNumber, x => x.Quantity, TestContext.Current.CancellationToken);

        byLot["LOT-OKT"].ShouldBe(6m);   // muddati YAQIN partiya kamaydi
        byLot["LOT-DEK"].ShouldBe(10m);  // uzoq muddatliga TEGILMADI

        // Chiqim tovarni kompaniyadan olib chiqadi — partiya qoldig'i ham kamayadi.
        decimal remaining = await verify.Db.Batches
            .Where(b => b.LotNumber == "LOT-OKT")
            .Select(b => b.RemainingQuantity)
            .FirstAsync(TestContext.Current.CancellationToken);
        remaining.ShouldBe(6m);
    }

    [Fact]
    public async Task Qoldiq_yetmasa_chiqim_YARATISHDA_xato_beradi_va_hujjat_saqlanmaydi()
    {
        TestTenant tenant = await NewTenantAsync();

        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db);
        Product product = await TestData.AddProductAsync(scope.Db, "Qaymoq 500 g");
        await TestData.AddStockAsync(scope.Db, warehouse, product, 10m);

        AppException error = await Should.ThrowAsync<AppException>(async () =>
            await scope.Service<ITransferService>().CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = warehouse.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 100m, UnitPrice = 15000m }],
            }));

        // Shablon — tarjima KALITI (AppException izohi), shuning uchun aynan tekshiriladi.
        error.MessageTemplate.ShouldBe(
            "Insufficient available stock for product {0}: need {1:N2}, available {2:N2}");

        // Argumentlar tayyor matndan emas, ro'yxatdan o'lchanadi: `{1:N2}` ning ko'rinishi
        // so'rov MADANIYATIGA bog'liq (100.00 / 100,00) va test uni o'lchamasligi kerak.
        error.MessageArgs.Length.ShouldBe(3);
        error.MessageArgs[0].ShouldBe("Qaymoq 500 g");
        error.MessageArgs[1].ShouldBe(100m);
        error.MessageArgs[2].ShouldBe(10m);

        // DARVOZA: yaratishdagi tekshiruv olib tashlansa hujjat `Pending` holida saqlanib qolardi
        // (xato faqat tasdiqda chiqardi) — shunda bu da'vo qizil beradi.
        int saved = await scope.Db.Transfers.CountAsync(TestContext.Current.CancellationToken);
        saved.ShouldBe(0);
    }

    [Fact]
    public async Task Kirim_tasdiqlansa_yangi_partiya_va_qoldiq_qatori_yaratiladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid productId;
        Guid warehouseId;
        DateTime beforeConfirm;
        DateTime afterConfirm;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
            Product product = await TestData.AddProductAsync(scope.Db, "Un 50 kg", shelfLifeDays: 30);
            productId = product.Id;
            warehouseId = warehouse.Id;

            ITransferService transfers = scope.Service<ITransferService>();
            TransferDto created = await transfers.CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Incoming,
                ToWarehouseId = warehouse.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 5m, UnitPrice = 120000m }],
            });

            // Muddat tasdiq LAHZASIDAN hisoblanadi — chegarani shu yerda «qisib» olamiz.
            beforeConfirm = DateTime.UtcNow;
            await transfers.ConfirmAsync(created.Id);
            afterConfirm = DateTime.UtcNow;
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        Batch batch = await verify.Db.Batches
            .SingleAsync(b => b.ProductId == productId, TestContext.Current.CancellationToken);

        batch.LotNumber.ShouldStartWith("LOT-");
        batch.InitialQuantity.ShouldBe(5m);
        batch.RemainingQuantity.ShouldBe(5m);

        // `ExpiryDate = tasdiq sanasi + ShelfLifeDays` — aniq lahza emas, oraliq tekshiriladi.
        batch.ExpiryDate.ShouldNotBeNull();
        batch.ExpiryDate.Value.ShouldBeGreaterThanOrEqualTo(beforeConfirm.AddDays(30));
        batch.ExpiryDate.Value.ShouldBeLessThanOrEqualTo(afterConfirm.AddDays(30));

        WarehouseStock stock = await verify.Db.WarehouseStocks
            .SingleAsync(s => s.ProductId == productId, TestContext.Current.CancellationToken);

        stock.WarehouseId.ShouldBe(warehouseId);
        stock.BatchId.ShouldBe(batch.Id);
        stock.Quantity.ShouldBe(5m);
    }

    [Fact]
    public async Task Parallel_ikki_tasdiqdan_bittasi_otadi_va_zaxira_BIR_marta_kamayadi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid transferId;
        Guid productId;

        await using (WmsTenantScope setup = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(setup.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(setup.Db);
            Product product = await TestData.AddProductAsync(setup.Db, "Shakar 1 kg");
            productId = product.Id;

            // 10 dona: ikkinchi tasdiq zaxira YETMAGANI uchun emas, AYNAN versiya
            // to'qnashuvi tufayli yiqilsin (aks holda test boshqa narsani o'lchardi).
            await TestData.AddStockAsync(setup.Db, warehouse, product, 10m);

            transferId = (await setup.Service<ITransferService>().CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = warehouse.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 4m, UnitPrice = 12000m }],
            })).Id;
        }

        // `Task.WhenAll` ATAYLAB ishlatilmadi: unda kim avval `SaveChanges` ga yetishi OS
        // rejalashtiruvchisiga qolardi va test vaqti-vaqti bilan ikkala tasdiqni ham ketma-ket
        // bajarib yuborib, yashil-qizil o'ynardi. Bu yerda poyga QO'LDA qayta tiklanadi:
        // birinchi qamrov hujjatni O'QIB oladi (uning `xmin` i — «eski»), ikkinchisi to'liq
        // tasdiqlab commit qiladi, keyin birinchisi saqlashga uriniladi. EF ayniyat xaritasi
        // tufayli birinchi qamrov hujjatni hamon `Pending` ko'radi — prod'dagi poyganing aynan
        // shu holati — va `UPDATE ... WHERE xmin = @eski` 0 qator oladi.
        await using WmsTenantScope stale = _fixture.BeginScope(tenant);
        await stale.Db.Transfers.FirstAsync(t => t.Id == transferId, TestContext.Current.CancellationToken);

        await using (WmsTenantScope winner = _fixture.BeginScope(tenant))
        {
            TransferDto confirmed = await winner.Service<ITransferService>().ConfirmAsync(transferId);
            confirmed.Status.ShouldBe(TransferStatus.Confirmed);
        }

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            await stale.Service<ITransferService>().ConfirmAsync(transferId));

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        decimal quantity = await verify.Db.WarehouseStocks
            .Where(s => s.ProductId == productId)
            .Select(s => s.Quantity)
            .SingleAsync(TestContext.Current.CancellationToken);

        // 10 − 4: ikkinchi tasdiq tranzaksiyasi TO'LIQ qaytdi, yarim qo'llangan zaxira yo'q.
        quantity.ShouldBe(6m);
    }

    [Fact]
    public async Task Rad_etilgan_chiqim_zaxiraga_TEGMAYDI()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid productId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db);
            Product product = await TestData.AddProductAsync(scope.Db, "Tuz 1 kg");
            productId = product.Id;
            await TestData.AddStockAsync(scope.Db, warehouse, product, 10m);

            ITransferService transfers = scope.Service<ITransferService>();
            TransferDto created = await transfers.CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = warehouse.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 4m, UnitPrice = 3000m }],
            });

            TransferDto rejected = await transfers.RejectAsync(created.Id);
            rejected.Status.ShouldBe(TransferStatus.Rejected);
            rejected.ConfirmedAt.ShouldBeNull();
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        TransferStatus status = await verify.Db.Transfers
            .Select(t => t.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
        status.ShouldBe(TransferStatus.Rejected);

        decimal quantity = await verify.Db.WarehouseStocks
            .Where(s => s.ProductId == productId)
            .Select(s => s.Quantity)
            .SingleAsync(TestContext.Current.CancellationToken);
        quantity.ShouldBe(10m);
    }

    [Fact]
    public async Task Ichki_kochirishda_manba_kamayadi_va_maqsad_omborda_qator_paydo_boladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid sourceId;
        Guid targetId;
        Guid batchId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse source = await TestData.AddWarehouseAsync(scope.Db, "Manba", WarehouseType.Raw);
            Warehouse target = await TestData.AddWarehouseAsync(scope.Db, "Maqsad");
            Product product = await TestData.AddProductAsync(scope.Db, "Yog' 5 l");
            sourceId = source.Id;
            targetId = target.Id;

            (Batch batch, _) = await TestData.AddStockAsync(scope.Db, source, product, 10m);
            batchId = batch.Id;

            ITransferService transfers = scope.Service<ITransferService>();
            TransferDto created = await transfers.CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Internal,
                FromWarehouseId = source.Id,
                ToWarehouseId = target.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 4m, UnitPrice = 0m }],
            });

            await transfers.ConfirmAsync(created.Id);
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        Dictionary<Guid, decimal> byWarehouse = await verify.Db.WarehouseStocks
            .Where(s => s.BatchId == batchId)
            .ToDictionaryAsync(s => s.WarehouseId, s => s.Quantity, TestContext.Current.CancellationToken);

        byWarehouse[sourceId].ShouldBe(6m);
        byWarehouse[targetId].ShouldBe(4m);  // maqsad ombor AYNAN o'sha partiyaga kredit qilindi

        // Tovar kompaniyadan chiqmadi — partiya qoldig'i o'zgarmaydi (chiqimdan farqi shunda).
        decimal remaining = await verify.Db.Batches
            .Where(b => b.Id == batchId)
            .Select(b => b.RemainingQuantity)
            .SingleAsync(TestContext.Current.CancellationToken);
        remaining.ShouldBe(10m);
    }
}
