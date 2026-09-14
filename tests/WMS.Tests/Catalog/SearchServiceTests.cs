using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Catalog;

/// <summary>
/// Nom qidiruvi (P2.1): kirill↔lotin farqi qidiruvga TA'SIR QILMAYDI va o'xshash
/// nomlarning hammasi nomzod bo'lib qaytadi.
/// </summary>
/// <remarks>
/// <para>
/// Darvoza: REJA §P2.1 qabul mezoni — «Snikers», «snickers», «Сникерс» uchtasi ham AYNAN
/// bitta mahsulotni BIRINCHI o'rinda beradi; «Plombir» bo'yicha uchta o'xshash nom
/// nomzod bo'ladi. Normalizator jadvali yoki <c>word_similarity</c> chegarasi buzilsa
/// shu testlar darhol qizil beradi.
/// </para>
/// <para>
/// ⚠️ <c>TestData.AddProductAsync</c> <c>NameSearch</c> ni to'ldirmaydi (u boshqa modulniki),
/// shuning uchun bu yerdagi yordamchilar mahsulot/kontragentni yaratgandan keyin ustunni
/// AYNAN prod servisidagi qoida bilan — <see cref="SearchNormalizer"/> orqali — qo'yib
/// saqlaydi. Servis orqali yaratish (<c>IProductService.CreateAsync</c>) yo'li ATAYLAB
/// tanlanmadi: u kategoriya/birlik DTO'sini talab qiladi va test tekshirmoqchi bo'lgan
/// narsadan (qidiruv) uzoqlashtiradi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class SearchServiceTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public SearchServiceTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData("Snikers")]
    [InlineData("snickers")]
    [InlineData("Сникерс")]
    public async Task Uch_xil_yozilgan_nom_bitta_mahsulotni_birinchi_qaytaradi(string query)
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Product target = await AddSearchableProductAsync(scope, "Snickers muzqaymoq");
        await AddSearchableProductAsync(scope, "Plombir 100g");
        await AddSearchableProductAsync(scope, "Krem-brule");

        IReadOnlyList<ProductSearchCandidate> found = await scope.Service<ISearchService>()
            .FindProductsAsync(query, cancellationToken: TestContext.Current.CancellationToken);

        found.ShouldNotBeEmpty();
        found[0].Id.ShouldBe(target.Id);
        found[0].Name.ShouldBe("Snickers muzqaymoq");
        found[0].Score.ShouldBeGreaterThan(SearchService_MinimumScore);
    }

    [Fact]
    public async Task Plombir_boyicha_uchta_oxshash_nom_nomzod_boladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        await AddSearchableProductAsync(scope, "Plombir 100g");
        await AddSearchableProductAsync(scope, "Plombir vanilli");
        await AddSearchableProductAsync(scope, "Пломбир шоколад");

        // Butunlay begona nom — nomzodlar orasiga tushmasligi kerak.
        await AddSearchableProductAsync(scope, "Snickers muzqaymoq");

        IReadOnlyList<ProductSearchCandidate> found = await scope.Service<ISearchService>()
            .FindProductsAsync("Plombir", cancellationToken: TestContext.Current.CancellationToken);

        found.Count.ShouldBe(3);
        found.Select(c => c.Name).ShouldBe(
            ["Plombir 100g", "Plombir vanilli", "Пломбир шоколад"], ignoreOrder: true);
    }

    [Fact]
    public async Task Boshqa_tenant_mahsuloti_qidiruvda_korinmaydi()
    {
        TestTenant first = await _fixture.CreateTenantAsync();
        TestTenant second = await _fixture.CreateTenantAsync();

        await using (WmsTenantScope owner = _fixture.BeginScope(first))
        {
            await AddSearchableProductAsync(owner, "Snickers muzqaymoq");
        }

        await using WmsTenantScope other = _fixture.BeginScope(second);

        // Boshqa tenantda AYNAN shu nom bilan qidirilmoqda: 0 natija — RLS va global filtr
        // qidiruv yo'lida ham ishlayotganining yagona isboti.
        IReadOnlyList<ProductSearchCandidate> found = await other.Service<ISearchService>()
            .FindProductsAsync("Snickers", cancellationToken: TestContext.Current.CancellationToken);

        found.ShouldBeEmpty();
    }

    [Fact]
    public async Task Kontragent_kirillcha_yozilsa_ham_topiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Counterparty target = await TestData.AddCounterpartyAsync(scope.Db, "Supermarket Korzinka");
        target.NameSearch = SearchNormalizer.Normalize(target.Name);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IReadOnlyList<CounterpartySearchCandidate> found = await scope.Service<ISearchService>()
            .FindCounterpartiesAsync("Корзинка", cancellationToken: TestContext.Current.CancellationToken);

        found.ShouldNotBeEmpty();
        found[0].Id.ShouldBe(target.Id);
        found[0].Type.ShouldBe(target.Type);
    }

    [Fact]
    public async Task Bosh_sorov_hech_narsa_qaytarmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        await AddSearchableProductAsync(scope, "Plombir 100g");

        // Bo'sh (yoki faqat tinish belgisidan iborat) so'rov butun katalogni qaytarmasligi kerak.
        IReadOnlyList<ProductSearchCandidate> blank = await scope.Service<ISearchService>()
            .FindProductsAsync("   ", cancellationToken: TestContext.Current.CancellationToken);

        blank.ShouldBeEmpty();
    }

    /// <summary>Normalizator jadvali — SQL backfill bilan bir xil qoidada ekanining tez tekshiruvi.</summary>
    [Theory]
    [InlineData("Сникерс", "snikers")]
    [InlineData("Пломбир шоколад", "plombir shokolad")]
    [InlineData("Bo'g'irsoq", "bogirsoq")]
    [InlineData("Крем-брюле", "krem bryule")]
    [InlineData("  Plombir   100ml  ", "plombir 100ml")]
    public void Normalizator_kirillni_lotinga_ogiradi(string input, string expected)
        => SearchNormalizer.Normalize(input).ShouldBe(expected);

    /// <summary>Servisdagi chegara bilan bir xil — test o'z qiymatini o'ylab topmasin.</summary>
    private const double SearchService_MinimumScore = 0.4;

    /// <summary>
    /// Mahsulot yaratadi va <c>NameSearch</c> ni prod qoidasi bilan to'ldiradi.
    /// </summary>
    /// <param name="scope">Tenant qamrovi.</param>
    /// <param name="name">Mahsulot nomi.</param>
    /// <returns>Yaratilgan mahsulot.</returns>
    private static async Task<Product> AddSearchableProductAsync(WmsTenantScope scope, string name)
    {
        Product product = await TestData.AddProductAsync(scope.Db, name);
        product.NameSearch = SearchNormalizer.Normalize(name);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }
}
