using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Production;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Trade;

/// <summary>
/// Hujjat sanasi (P2.3), qisqa raqam (P2.4) va partiya tannarxi (P2.5) — hujjat
/// YARATILGANDA va TASDIQLANGANDA yoziladigan qiymatlar.
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak. Uchala maydon ham «jimgina noto'g'ri» bo'la oladigan turdan:
/// </para>
/// <list type="bullet">
/// <item><b>Sana.</b> <c>DocumentDate</c> <c>CreatedAt</c> ga almashib qolsa hech qanday
/// xato chiqmaydi — kecha kelgan kirim bugungi kunga tushadi va buni faqat oy oxirida,
/// hisobot omborchining daftariga mos kelmaganda sezishadi.</item>
/// <item><b>Raqam.</b> Hisoblagich noto'g'ri olinsa ikki hujjat BIR XIL «#7» ni oladi:
/// telefonda «yettinchi hujjat» deyilganda qaysi biri ekani aniqlanmay qoladi. Shuning
/// uchun bu yerda raqam PARALLEL yaratishda o'lchanadi — ketma-ket kodda nosozlik ko'rinmaydi.</item>
/// <item><b>Tannarx.</b> Partiyaning tannarxi yozilmasa foyda sotuv summasiga TENG bo'lib
/// ko'rinadi (tannarx nol), ya'ni hisobot zararni ham foyda deb ko'rsatardi.</item>
/// </list>
/// <para>
/// Testlar HAQIQIY Postgres'da yuradi: raqam <c>UPDATE … RETURNING</c> qator qulfiga,
/// tannarx esa <c>numeric</c> yaxlitlashiga tayanadi — InMemory'da ikkalasi ham yo'q.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class TransferDocumentTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public TransferDocumentTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    /// <summary>Noyob kodli yangi test tenanti (kod tasodifiy v4 — sabab <c>TransferServiceTests</c> da).</summary>
    private Task<TestTenant> NewTenantAsync() =>
        _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");

    // ── P2.3: hujjat sanasi ──

    [Fact]
    public async Task Sana_berilmasa_hujjat_BUGUNGI_kun_bilan_yoziladi()
    {
        TestTenant tenant = await NewTenantAsync();

        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
        Product product = await TestData.AddProductAsync(scope.Db, "Un 50 kg");

        TransferDto created = await scope.Service<ITransferService>()
            .CreateAsync(user.Id, IncomingOf(warehouse, product));

        // Kun boshi, UTC — `DocumentDates.Resolve` qoidasi (vaqt qismi saqlanmaydi).
        created.DocumentDate.ShouldBe(DateTime.UtcNow.Date);

        // Sukut manba — ekran: mijoz o'zini «AI» deb ko'rsata olmaydi (DTO'da `Source` yo'q).
        created.Source.ShouldBe(DocumentSource.Ui);
    }

    [Fact]
    public async Task Kelajak_sana_HECH_KIMGA_ruxsat_emas_va_hujjat_saqlanmaydi()
    {
        TestTenant tenant = await NewTenantAsync();

        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
        Product product = await TestData.AddProductAsync(scope.Db, "Shakar 1 kg");

        // Orqaga sana ruxsati BERILGAN — test aynan «kelajak» qoidasini o'lchasin: bu ruxsat
        // kelajakni ochmaydi (ochsa, kelajakdagi sotuv bugungi hisobotdan yo'qolib ketardi).
        scope.AsUser(user.Id, WmsPermissions.DocumentsBackdate);

        CreateTransferDto dto = IncomingOf(warehouse, product);
        dto.DocumentDate = DateTime.UtcNow.Date.AddDays(1);

        AppException error = await Should.ThrowAsync<AppException>(async () =>
            await scope.Service<ITransferService>().CreateAsync(user.Id, dto));

        error.MessageTemplate.ShouldBe(DocumentDates.FutureMessage);

        // DARVOZA: tekshiruv yaratishdan KEYIN qolib ketsa hujjat baribir yozilardi.
        int saved = await scope.Db.Transfers.CountAsync(TestContext.Current.CancellationToken);
        saved.ShouldBe(0);
    }

    [Fact]
    public async Task Orqaga_sana_RUXSATSIZ_rad_etiladi()
    {
        TestTenant tenant = await NewTenantAsync();

        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
        Product product = await TestData.AddProductAsync(scope.Db, "Tuz 1 kg");

        // Ruxsatlar ATAYLAB bo'sh: «hammasiga ruxsat» sukuti bu darvozani jimgina yashil qilardi.
        scope.AsUser(user.Id);

        CreateTransferDto dto = IncomingOf(warehouse, product);
        dto.DocumentDate = DateTime.UtcNow.Date.AddDays(-3);

        ForbiddenException error = await Should.ThrowAsync<ForbiddenException>(async () =>
            await scope.Service<ITransferService>().CreateAsync(user.Id, dto));

        error.MessageTemplate.ShouldBe(DocumentDates.BackdateForbiddenMessage);
        error.MessageArgs.Length.ShouldBe(1);
        error.MessageArgs[0].ShouldBe(WmsPermissions.DocumentsBackdate);

        int saved = await scope.Db.Transfers.CountAsync(TestContext.Current.CancellationToken);
        saved.ShouldBe(0);
    }

    [Fact]
    public async Task Orqaga_sana_RUXSAT_bilan_otadi_va_ayni_kun_yoziladi()
    {
        TestTenant tenant = await NewTenantAsync();
        DateTime backdated = DateTime.UtcNow.Date.AddDays(-3);
        Guid transferId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
            Product product = await TestData.AddProductAsync(scope.Db, "Yog' 5 l");

            scope.AsUser(user.Id, WmsPermissions.DocumentsBackdate);

            CreateTransferDto dto = IncomingOf(warehouse, product);
            dto.DocumentDate = backdated;

            TransferDto created = await scope.Service<ITransferService>().CreateAsync(user.Id, dto);
            created.DocumentDate.ShouldBe(backdated);
            transferId = created.Id;
        }

        // Bazadagi qiymat o'lchansin: EF kuzatuvidagi nusxa emas (ustun `timestamptz`).
        await using WmsTenantScope verify = _fixture.BeginScope(tenant);
        Transfer stored = await verify.Db.Transfers
            .SingleAsync(t => t.Id == transferId, TestContext.Current.CancellationToken);

        stored.DocumentDate.ShouldBe(backdated);

        // ⚠️ `CreatedAt` — audit izi va u BUGUN qoladi: orqaga sanalash yozuv lahzasini
        // «qayta yozmaydi», aks holda kim va qachon kiritgani izsiz yo'qolardi.
        stored.CreatedAt.Date.ShouldBe(DateTime.UtcNow.Date);
    }

    // ── P2.4: qisqa raqam ──

    [Fact]
    public async Task Parallel_yigirma_hujjat_yigirma_XIL_va_ketma_ket_raqam_oladi()
    {
        const int count = 20;
        TestTenant tenant = await NewTenantAsync();
        Guid userId;
        Guid warehouseId;
        Guid productId;

        await using (WmsTenantScope setup = _fixture.BeginScope(tenant))
        {
            userId = (await TestData.AddUserAsync(setup.Db)).Id;
            Warehouse warehouse = await TestData.AddWarehouseAsync(setup.Db, "Xom ashyo", WarehouseType.Raw);
            warehouseId = warehouse.Id;
            productId = (await TestData.AddProductAsync(setup.Db, "Quruq sut 25 kg")).Id;
        }

        // Har hujjat O'Z DI qamrovida: `WmsDbContext` bitta oqim uchun mo'ljallangan, shuning
        // uchun bu yerda haqiqiy poyga ulanishlar darajasida bo'ladi — hisoblagich qatorining
        // qulfi (`UPDATE … RETURNING`) ishlamasa ikki so'rov bir xil raqamni olardi.
        Task<TransferDto>[] creations = [.. Enumerable.Range(0, count).Select(_ => CreateOneAsync())];
        TransferDto[] created = await Task.WhenAll(creations);

        int[] numbers = [.. created.Select(t => t.Number).Order()];
        numbers.Distinct().Count().ShouldBe(count);

        // Hisoblagich 1 dan boshlanadi va bo'shliqsiz: bu yerda hech bir tranzaksiya qaytmagan.
        numbers.ShouldBe([.. Enumerable.Range(1, count)]);

        async Task<TransferDto> CreateOneAsync()
        {
            await using WmsTenantScope scope = _fixture.BeginScope(tenant);
            return await scope.Service<ITransferService>().CreateAsync(userId, new CreateTransferDto
            {
                Type = TransferType.Incoming,
                ToWarehouseId = warehouseId,
                Items = [new CreateTransferItemDto { ProductId = productId, Quantity = 1m, UnitPrice = 1000m }],
            });
        }
    }

    // ── P2.5: partiya va qator tannarxi ──

    [Fact]
    public async Task Kirim_tasdiqlansa_partiya_tannarxi_KIRIM_NARXIDAN_yoziladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid pricedId;
        Guid freeId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
            Product priced = await TestData.AddProductAsync(scope.Db, "Quruq sut 25 kg");
            Product free = await TestData.AddProductAsync(scope.Db, "Namuna qadoq");
            pricedId = priced.Id;
            freeId = free.Id;

            ITransferService transfers = scope.Service<ITransferService>();
            TransferDto created = await transfers.CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Incoming,
                ToWarehouseId = warehouse.Id,
                Items =
                [
                    new CreateTransferItemDto { ProductId = priced.Id, Quantity = 4m, UnitPrice = 25000m },
                    new CreateTransferItemDto { ProductId = free.Id, Quantity = 2m, UnitPrice = 0m },
                ],
            });

            await transfers.ConfirmAsync(created.Id);
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        decimal? priceCost = await verify.Db.Batches
            .Where(b => b.ProductId == pricedId).Select(b => b.UnitCost)
            .SingleAsync(TestContext.Current.CancellationToken);
        priceCost.ShouldBe(25000m);

        // Nol narx «tekin keldi» emas, «narx kiritilmagan» — tannarx NOMA'LUM bo'lib qoladi.
        // 0 yozilsa hisobot bu partiyani sof foyda deb ko'rsatardi.
        decimal? freeCost = await verify.Db.Batches
            .Where(b => b.ProductId == freeId).Select(b => b.UnitCost)
            .SingleAsync(TestContext.Current.CancellationToken);
        freeCost.ShouldBeNull();
    }

    [Fact]
    public async Task Chiqim_qatoriga_FEFO_partiyalarining_ORTACHA_tannarxi_yoziladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid transferId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Tayyor mahsulot");
            Product product = await TestData.AddProductAsync(scope.Db, "Plombir 100 ml");

            // Muddati YAQIN partiya arzon, uzoq muddatlisi qimmat: 15 dona olinganda FEFO
            // 10 tasini arzonidan, 5 tasini qimmatidan oladi.
            (Batch cheap, _) = await TestData.AddStockAsync(scope.Db, warehouse, product, 10m,
                expiry: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "LOT-OKT");
            (Batch expensive, _) = await TestData.AddStockAsync(scope.Db, warehouse, product, 10m,
                expiry: new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "LOT-DEK");

            cheap.UnitCost = 1000m;
            expensive.UnitCost = 1200m;
            await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

            ITransferService transfers = scope.Service<ITransferService>();
            TransferDto created = await transfers.CreateAsync(user.Id, new CreateTransferDto
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = warehouse.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 15m, UnitPrice = 5000m }],
            });

            transferId = created.Id;
            await transfers.ConfirmAsync(transferId);
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        decimal? unitCost = await verify.Db.TransferItems
            .Where(i => i.TransferId == transferId).Select(i => i.UnitCost)
            .SingleAsync(TestContext.Current.CancellationToken);

        // (10 × 1000 + 5 × 1200) / 15 = 1066.67. ⚠️ ODDIY o'rtacha 1100 bo'lardi — test
        // aynan MIQDOR bo'yicha og'irlanganini o'lchaydi.
        unitCost.ShouldBe(1066.67m);
    }

    /// <summary>Eng sodda kirim hujjati (sana testlari uchun — zaxira tekshiruvi yo'q yo'l).</summary>
    private static CreateTransferDto IncomingOf(Warehouse warehouse, Product product) => new()
    {
        Type = TransferType.Incoming,
        ToWarehouseId = warehouse.Id,
        Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 3m, UnitPrice = 10000m }],
    };
}

/// <summary>
/// Ishlab chiqarilgan partiyaning tannarxi (P2.5): sarflangan xomashyo qiymati bosqichda
/// yig'iladi va buyurtma yakunlanganda tayyor partiyaga bir birlikka bo'lib yoziladi.
/// </summary>
/// <remarks>
/// ⚠️ Bu yo'l IKKI SO'ROVGA bo'lingan: xomashyo bosqich bajarilganda sarflanadi, tayyor
/// partiya esa buyurtma yakunlanganda tug'iladi. Oradagi qiymat <c>StageExecution.MaterialCost</c>
/// da saqlanadi — aynan shu ulanish uzilsa hech qanday xato chiqmaydi, tayyor mahsulot
/// shunchaki tannarxsiz qoladi va foyda hisoboti butun sotuv summasini foyda deb ko'rsatadi.
/// Shuning uchun test bosqichdagi qiymatni ham, yakundagi natijani ham o'lchaydi.
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class ProductionCostTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public ProductionCostTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    private Task<TestTenant> NewTenantAsync() =>
        _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");

    [Fact]
    public async Task Tayyor_partiya_tannarxi_SARFLANGAN_xomashyo_qiymatidan_chiqadi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid outputProductId;
        Guid orderId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            Warehouse raw = await TestData.AddWarehouseAsync(scope.Db, "Xom ashyo", WarehouseType.Raw);
            await TestData.AddWarehouseAsync(scope.Db, "Tayyor mahsulot");

            Product milk = await TestData.AddProductAsync(scope.Db, "Quruq sut 25 kg");
            Product sugar = await TestData.AddProductAsync(scope.Db, "Shakar 1 kg");
            Product output = await TestData.AddProductAsync(scope.Db, "Plombir 100 ml");
            outputProductId = output.Id;

            // Ikki XIL tannarxli xomashyo: bitta narxdan hisoblansa test farqni ko'rmasdi.
            (Batch milkBatch, _) = await TestData.AddStockAsync(scope.Db, raw, milk, 100m);
            (Batch sugarBatch, _) = await TestData.AddStockAsync(scope.Db, raw, sugar, 100m);
            milkBatch.UnitCost = 1000m;
            sugarBatch.UnitCost = 500m;
            await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

            IProductionService production = scope.Service<IProductionService>();
            ProductionStageDto stage = await production.CreateStageAsync(
                new CreateProductionStageDto { Name = "Aralashma", OrderNumber = 1 });

            // Retseptning BITTA partiyasi — 10 dona: 2 birlik sut + 1 birlik shakar.
            ProductionRecipeDto recipe = await production.CreateRecipeAsync(new CreateRecipeDto
            {
                Name = "Plombir retsepti",
                OutputProductId = output.Id,
                OutputQuantity = 10m,
                OutputUnitId = output.UnitId,
                Stages =
                [
                    new CreateRecipeStageDto
                    {
                        StageId = stage.Id,
                        OrderNumber = 1,
                        Inputs =
                        [
                            new CreateRecipeStageItemDto { ProductId = milk.Id, Quantity = 2m, UnitId = milk.UnitId },
                            new CreateRecipeStageItemDto { ProductId = sugar.Id, Quantity = 1m, UnitId = sugar.UnitId },
                        ],
                    },
                ],
            });

            // Reja = retsept partiyasi (koeffitsiyent 1): 2 × 1000 + 1 × 500 = 2500 so'm sarf.
            ProductionOrderDto order = await production.CreateOrderAsync(new CreateProductionOrderDto
            {
                RecipeId = recipe.Id,
                PlannedQuantity = 10m,
                PlannedStartDate = DateTime.UtcNow,
            });

            orderId = order.Id;
            order.Number.ShouldBe(1);   // qisqa raqam buyurtmaga ham beriladi (P2.4)

            await production.StartOrderAsync(orderId);
            await production.ExecuteStageAsync(orderId, order.StageExecutions[0].Id,
                new ExecuteStageDto { ActualQuantity = 10m });
            await production.CompleteOrderAsync(orderId);
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        // Bosqichdagi ulanish: sarf qiymati SAQLANDI (yakun uni boshqa so'rovda o'qiydi).
        decimal? stageCost = await verify.Db.StageExecutions
            .Where(se => se.ProductionOrderId == orderId).Select(se => se.MaterialCost)
            .SingleAsync(TestContext.Current.CancellationToken);
        stageCost.ShouldBe(2500m);

        // Yakundagi natija: 2500 / 10 dona = 250 so'm bir dona tannarx.
        Batch produced = await verify.Db.Batches
            .SingleAsync(b => b.ProductId == outputProductId, TestContext.Current.CancellationToken);

        produced.LotNumber.ShouldStartWith("PROD-1-");   // Guid emas, qisqa raqam
        produced.UnitCost.ShouldBe(250m);
    }

    [Fact]
    public async Task Xomashyo_sarflanmagan_buyurtmada_tannarx_NULL_qoladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid outputProductId;
        Guid orderId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            await TestData.AddWarehouseAsync(scope.Db, "Tayyor mahsulot");
            Product output = await TestData.AddProductAsync(scope.Db, "Qadoqlangan muz");
            outputProductId = output.Id;

            IProductionService production = scope.Service<IProductionService>();
            ProductionStageDto stage = await production.CreateStageAsync(
                new CreateProductionStageDto { Name = "Qadoqlash", OrderNumber = 1 });

            // Kirimsiz bosqich: sarf yo'q, demak tannarx ham NOMA'LUM.
            ProductionRecipeDto recipe = await production.CreateRecipeAsync(new CreateRecipeDto
            {
                Name = "Kirimsiz retsept",
                OutputProductId = output.Id,
                OutputQuantity = 10m,
                OutputUnitId = output.UnitId,
                Stages = [new CreateRecipeStageDto { StageId = stage.Id, OrderNumber = 1 }],
            });

            ProductionOrderDto order = await production.CreateOrderAsync(new CreateProductionOrderDto
            {
                RecipeId = recipe.Id,
                PlannedQuantity = 10m,
                PlannedStartDate = DateTime.UtcNow,
            });

            orderId = order.Id;
            await production.StartOrderAsync(orderId);
            await production.ExecuteStageAsync(orderId, order.StageExecutions[0].Id,
                new ExecuteStageDto { ActualQuantity = 10m });
            await production.CompleteOrderAsync(orderId);
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        decimal? stageCost = await verify.Db.StageExecutions
            .Where(se => se.ProductionOrderId == orderId).Select(se => se.MaterialCost)
            .SingleAsync(TestContext.Current.CancellationToken);
        stageCost.ShouldBeNull();

        // ⚠️ DARVOZA: bu yerda 0 yozilsa tayyor mahsulot «tekin ishlab chiqarilgan» bo'lib
        // qolardi va foyda hisoboti butun sotuv summasini foyda deb ko'rsatardi. Noma'lum
        // tannarx OCHIQ `null` bo'lib turishi kerak.
        decimal? unitCost = await verify.Db.Batches
            .Where(b => b.ProductId == outputProductId).Select(b => b.UnitCost)
            .SingleAsync(TestContext.Current.CancellationToken);
        unitCost.ShouldBeNull();
    }
}
