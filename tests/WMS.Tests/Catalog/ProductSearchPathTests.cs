using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Catalog;

/// <summary>
/// UI AYNAN yuradigan yo'l: <c>GET /api/products?search=</c> va
/// <c>GET /api/counterparties?search=</c> ortidagi servis metodlari.
/// </summary>
/// <remarks>
/// <para>
/// Nega alohida kerak: qidiruvning o'zi <see cref="ISearchService"/> da sinaladi, lekin
/// ekran uni TO'G'RIDAN-TO'G'RI chaqirmaydi — ro'yxat metodlari orqali o'tadi. Ikkisi
/// orasidagi ulanish uzilsa (masalan `search` parametri servisga berilmay qolsa)
/// qidiruv testlari HAMON yashil bo'lardi, ekran esa eski to'liq ro'yxatni ko'rsatardi.
/// </para>
/// <para>
/// ⚠️ Ikkinchi da'vo — <c>name_search</c> ustuni YOZILISHI: mahsulot ekran orqali
/// yaratilganda normalizator chaqirilmasa, yangi mahsulot qidiruvda umuman topilmasdi
/// (migratsiyadagi backfill faqat ESKI qatorlarni to'g'rilaydi).
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class ProductSearchPathTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public ProductSearchPathTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Servis_orqali_yaratilgan_mahsulot_kirillcha_sorovda_topiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Category category = await TestData.AddCategoryAsync(scope.Db, "Shirinliklar");
        Unit unit = await scope.Db.Units.FirstAsync(TestContext.Current.CancellationToken);

        IProductService products = scope.Service<IProductService>();
        await products.CreateAsync(new CreateProductDto
        {
            Name = "Snikers",
            CategoryId = category.Id,
            UnitId = unit.Id,
            Type = ProductType.Finished,
        });
        await products.CreateAsync(new CreateProductDto
        {
            Name = "Quyuq sut",
            CategoryId = category.Id,
            UnitId = unit.Id,
            Type = ProductType.Finished,
        });

        // Ekran shu metodni chaqiradi — kirillcha yozilgan so'rov bilan.
        List<ProductDto> found = await products.GetAllAsync(search: "Сникерс");

        found.Count.ShouldBe(1);
        found[0].Name.ShouldBe("Snikers");

        // Qidiruvsiz — ikkalasi ham (eski xatti-harakat buzilmagan).
        List<ProductDto> all = await products.GetAllAsync();
        all.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Nom_ozgartirilsa_qidiruv_ustuni_ham_yangilanadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Category category = await TestData.AddCategoryAsync(scope.Db);
        Unit unit = await scope.Db.Units.FirstAsync(TestContext.Current.CancellationToken);

        IProductService products = scope.Service<IProductService>();
        ProductDto created = await products.CreateAsync(new CreateProductDto
        {
            Name = "Plombir",
            CategoryId = category.Id,
            UnitId = unit.Id,
            Type = ProductType.Finished,
        });

        await products.UpdateAsync(created.Id, new UpdateProductDto
        {
            Name = "Шоколад",
            CategoryId = category.Id,
            UnitId = unit.Id,
            Type = ProductType.Finished,
        });

        // Eski nom bo'yicha endi topilmaydi, yangisi bo'yicha topiladi.
        (await products.GetAllAsync(search: "plombir")).ShouldBeEmpty();
        List<ProductDto> found = await products.GetAllAsync(search: "shokolad");
        found.Count.ShouldBe(1);
        found[0].Name.ShouldBe("Шоколад");
    }

    [Fact]
    public async Task Kontragent_royxatida_tur_filtri_va_qidiruv_BIRGA_ishlaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        await TestData.AddCounterpartyAsync(scope.Db, "Korzinka", CounterpartyType.Client);
        await TestData.AddCounterpartyAsync(scope.Db, "Korzinka Logistika", CounterpartyType.Supplier);

        // ⚠️ `TestData` ustunni to'ldirmaydi (u domen yordamchisi) — ekran yo'li servis
        // orqali yozadi, shuning uchun bu yerda qidiruv ustuni qo'lda to'ldiriladi.
        foreach (Counterparty row in await scope.Db.Counterparties.ToListAsync(TestContext.Current.CancellationToken))
        {
            row.NameSearch = SearchNormalizer.Normalize(row.Name);
        }

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        ICounterpartyService counterparties = scope.Service<ICounterpartyService>();

        List<CounterpartyDto> clients = await counterparties.GetAllAsync(CounterpartyType.Client, "корзинка");
        List<CounterpartyDto> everyone = await counterparties.GetAllAsync(null, "корзинка");

        clients.Count.ShouldBe(1);
        clients[0].Name.ShouldBe("Korzinka");
        everyone.Count.ShouldBe(2);
    }
}
