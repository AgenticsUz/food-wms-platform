using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Catalog;

/// <summary>
/// FEFO yechuvchining ikki yuzi — «qancha bor» (<see cref="IStockAllocator.GetAvailableAsync"/>)
/// va «yechib ber» (<see cref="IStockAllocator.DeductFefoAsync"/>) — AYNAN bir xil shart bilan
/// ishlashi tekshiriladi.
/// </summary>
/// <remarks>
/// Darvoza (2026-09-14, `docs/XATOLAR-2026-09-14.md` §3): prod'da «omborda yetarli qoldiq yo'q»
/// xatosi tasdiq bosqichida chiqqan edi. Ikki yuza ikki xil shartda ishlasa AYNAN shu hol
/// qaytadi: yaratishdagi tekshiruv «yetadi» deydi, tasdiq esa yiqiladi. Shuning uchun bu yerdagi
/// testlar ikkalasini HAR safar juftlab o'lchaydi; shartlar ajralsa qizil beradi.
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class StockAllocatorTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public StockAllocatorTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ochirilgan_partiyali_qator_FEFO_da_korinadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy");
        Product product = await TestData.AddProductAsync(scope.Db, "Muzqaymoq");
        (Batch batch, WarehouseStock stock) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 40m, DateTime.UtcNow.AddDays(30));

        // Partiya YUMSHOQ o'chiriladi: tovar omborda qolgan, faqat partiya kartasi yopilgan.
        batch.IsDeleted = true;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IStockAllocator allocator = scope.Service<IStockAllocator>();

        Dictionary<Guid, decimal> available = await allocator.GetAvailableAsync(
            [product.Id], warehouse.Id, TestContext.Current.CancellationToken);
        FefoAllocation allocation = await allocator.DeductFefoAsync(
            product.Id, 10m, warehouse.Id, cancellationToken: TestContext.Current.CancellationToken);

        available.GetValueOrDefault(product.Id).ShouldBe(40m);
        allocation.IsSatisfied.ShouldBeTrue();
        allocation.Lines.Single().Stock.Id.ShouldBe(stock.Id);
        stock.Quantity.ShouldBe(30m);
    }

    [Fact]
    public async Task Ochirilgan_qoldiq_qatori_esa_hisobga_olinmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy");
        Product product = await TestData.AddProductAsync(scope.Db, "Sut");
        (_, WarehouseStock stock) = await TestData.AddStockAsync(scope.Db, warehouse, product, 25m);

        // Bu safar QOLDIQ qatorining o'zi o'chirilgan — bunday qator hech qayerda ko'rinmaydi.
        stock.IsDeleted = true;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IStockAllocator allocator = scope.Service<IStockAllocator>();

        Dictionary<Guid, decimal> available = await allocator.GetAvailableAsync(
            [product.Id], warehouse.Id, TestContext.Current.CancellationToken);
        FefoAllocation allocation = await allocator.DeductFefoAsync(
            product.Id, 1m, warehouse.Id, cancellationToken: TestContext.Current.CancellationToken);

        available.GetValueOrDefault(product.Id).ShouldBe(0m);
        allocation.IsSatisfied.ShouldBeFalse();
        allocation.Shortfall.ShouldBe(1m);
    }

    [Fact]
    public async Task Ikki_yuza_bir_xil_javob_beradi_aralash_qatorlarda()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Aralash");
        Product product = await TestData.AddProductAsync(scope.Db, "Plombir");

        // (1) oddiy qator, (2) o'chirilgan partiyali qator, (3) to'liq band qilingan qator,
        // (4) o'chirilgan qoldiq qatori — mavjudlik = 20 + 15 + 0 + 0.
        await TestData.AddStockAsync(scope.Db, warehouse, product, 20m, DateTime.UtcNow.AddDays(10));
        (Batch deletedBatch, _) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 15m, DateTime.UtcNow.AddDays(20));
        await TestData.AddStockAsync(scope.Db, warehouse, product, 12m, DateTime.UtcNow.AddDays(5), reserved: 12m);
        (_, WarehouseStock deletedStock) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 100m, DateTime.UtcNow.AddDays(1));

        deletedBatch.IsDeleted = true;
        deletedStock.IsDeleted = true;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IStockAllocator allocator = scope.Service<IStockAllocator>();

        Dictionary<Guid, decimal> available = await allocator.GetAvailableAsync(
            [product.Id], warehouse.Id, TestContext.Current.CancellationToken);
        decimal reported = available.GetValueOrDefault(product.Id);

        // «Qancha bor» deyilgan miqdor to'liq yechilishi SHART — bir donaga ham farq qilmasin.
        FefoAllocation exact = await allocator.DeductFefoAsync(
            product.Id, reported, warehouse.Id, cancellationToken: TestContext.Current.CancellationToken);

        reported.ShouldBe(35m);
        exact.IsSatisfied.ShouldBeTrue();
        exact.Lines.Sum(l => l.Quantity).ShouldBe(35m);
    }

    [Fact]
    public async Task Muddatsiz_partiya_FEFO_da_oxirida_turadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "FEFO");
        Product product = await TestData.AddProductAsync(scope.Db, "Shokolad");

        (_, WarehouseStock noExpiry) = await TestData.AddStockAsync(scope.Db, warehouse, product, 10m);
        (_, WarehouseStock soon) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 10m, DateTime.UtcNow.AddDays(3));
        (_, WarehouseStock later) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 10m, DateTime.UtcNow.AddDays(60));

        FefoAllocation allocation = await scope.Service<IStockAllocator>().DeductFefoAsync(
            product.Id, 25m, warehouse.Id, cancellationToken: TestContext.Current.CancellationToken);

        allocation.IsSatisfied.ShouldBeTrue();
        allocation.Lines.Select(l => l.Stock.Id).ShouldBe([soon.Id, later.Id, noExpiry.Id]);
        allocation.Lines.Select(l => l.Quantity).ShouldBe([10m, 10m, 5m]);
    }

    [Fact]
    public async Task Yetmasa_hech_qanday_qator_ozgarmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Kam");
        Product product = await TestData.AddProductAsync(scope.Db, "Qaymoq");
        (Batch batch, WarehouseStock stock) = await TestData.AddStockAsync(
            scope.Db, warehouse, product, 5m, DateTime.UtcNow.AddDays(7));

        FefoAllocation allocation = await scope.Service<IStockAllocator>().DeductFefoAsync(
            product.Id, 8m, warehouse.Id, cancellationToken: TestContext.Current.CancellationToken);

        allocation.IsSatisfied.ShouldBeFalse();
        allocation.Shortfall.ShouldBe(3m);
        allocation.Lines.ShouldBeEmpty();

        // Yarim yechilgan holat kuzatuvda qolmasin: keyingi `SaveChanges` uni yozib yuborardi.
        stock.Quantity.ShouldBe(5m);
        batch.RemainingQuantity.ShouldBe(5m);
    }

    [Fact]
    public async Task Boshqa_ombordagi_qoldiq_hisobga_olinmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse source = await TestData.AddWarehouseAsync(scope.Db, "Manba");
        Warehouse other = await TestData.AddWarehouseAsync(scope.Db, "Boshqa");
        Product product = await TestData.AddProductAsync(scope.Db, "Tvorog");

        await TestData.AddStockAsync(scope.Db, other, product, 50m, DateTime.UtcNow.AddDays(10));

        IStockAllocator allocator = scope.Service<IStockAllocator>();

        Dictionary<Guid, decimal> inSource = await allocator.GetAvailableAsync(
            [product.Id], source.Id, TestContext.Current.CancellationToken);
        Dictionary<Guid, decimal> everywhere = await allocator.GetAvailableAsync(
            [product.Id], null, TestContext.Current.CancellationToken);

        inSource.GetValueOrDefault(product.Id).ShouldBe(0m);
        everywhere.GetValueOrDefault(product.Id).ShouldBe(50m);
    }
}
