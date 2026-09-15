using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Catalog;

/// <summary>
/// Qadoq (P2.7, Variant A): mahsulot kartasidagi «1 quti = N dona» maydonlari.
/// </summary>
/// <remarks>
/// ⚠️ Backend faqat maydonni SAQLAYDI: qoldiq, FEFO va hisobotlar doim asosiy birlikda,
/// qutidan donaga o'girish formada bo'ladi. Shuning uchun bu yerda konversiya emas,
/// JUFTLIK qoidasi tekshiriladi — yarim to'ldirilgan qadoq («50» — nimaning ellikta?)
/// keyinchalik AI ham, forma ham o'qiy olmaydigan ma'lumot bo'lardi.
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class PackSizeTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public PackSizeTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Qadoq_yaratishda_va_tahrirda_DTOda_qaytadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        (Guid categoryId, Guid unitId) = await CatalogAsync(scope);
        IProductService products = scope.Service<IProductService>();

        ProductDto created = await products.CreateAsync(new CreateProductDto
        {
            Name = "Plombir 100ml",
            CategoryId = categoryId,
            UnitId = unitId,
            Type = ProductType.Finished,
            PackSize = 24m,
            PackUnit = "  quti  ",
        });

        created.PackSize.ShouldBe(24m);
        // Bo'sh joylar kesiladi — «quti » va «quti» bir xil qadoq.
        created.PackUnit.ShouldBe("quti");

        // Ro'yxat proyeksiyasi (SQL) ham shu maydonlarni beradi.
        ProductDto listed = (await products.GetAllAsync()).Single(p => p.Id == created.Id);
        listed.PackSize.ShouldBe(24m);
        listed.PackUnit.ShouldBe("quti");

        ProductDto updated = await products.UpdateAsync(created.Id, new UpdateProductDto
        {
            Name = "Plombir 100ml",
            CategoryId = categoryId,
            UnitId = unitId,
            Type = ProductType.Finished,
            PackSize = 12m,
            PackUnit = "karobka",
        });

        updated.PackSize.ShouldBe(12m);
        updated.PackUnit.ShouldBe("karobka");

        // Ikkovi ham bo'sh — qadoqsiz mahsulot (eski xatti-harakat).
        ProductDto withoutPack = await products.UpdateAsync(created.Id, new UpdateProductDto
        {
            Name = "Plombir 100ml",
            CategoryId = categoryId,
            UnitId = unitId,
            Type = ProductType.Finished,
        });

        withoutPack.PackSize.ShouldBeNull();
        withoutPack.PackUnit.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Musbat_bolmagan_qadoq_rad_etiladi(int packSize)
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        (Guid categoryId, Guid unitId) = await CatalogAsync(scope);
        IProductService products = scope.Service<IProductService>();

        AppException error = await Should.ThrowAsync<AppException>(async () =>
            await products.CreateAsync(new CreateProductDto
            {
                Name = "Shakar",
                CategoryId = categoryId,
                UnitId = unitId,
                Type = ProductType.Raw,
                PackSize = packSize,
                PackUnit = "qop",
            }));

        error.MessageTemplate.ShouldBe("Pack size must be greater than zero");
    }

    [Fact]
    public async Task Qadoq_maydonlari_JUFT_boladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        (Guid categoryId, Guid unitId) = await CatalogAsync(scope);
        IProductService products = scope.Service<IProductService>();

        // Miqdor bor, nomi yo'q.
        AppException missingUnit = await Should.ThrowAsync<AppException>(async () =>
            await products.CreateAsync(new CreateProductDto
            {
                Name = "Shakar",
                CategoryId = categoryId,
                UnitId = unitId,
                Type = ProductType.Raw,
                PackSize = 50m,
                PackUnit = "   ",
            }));
        missingUnit.MessageTemplate.ShouldBe("Pack unit is required when pack size is set");

        // Nomi bor, miqdori yo'q.
        AppException missingSize = await Should.ThrowAsync<AppException>(async () =>
            await products.CreateAsync(new CreateProductDto
            {
                Name = "Shakar",
                CategoryId = categoryId,
                UnitId = unitId,
                Type = ProductType.Raw,
                PackUnit = "qop",
            }));
        missingSize.MessageTemplate.ShouldBe("Pack size is required when pack unit is set");

        // Xatoli urinishdan keyin bazada mahsulot qolmasin.
        (await products.GetAllAsync()).ShouldBeEmpty();
    }

    /// <summary>Kategoriya va birlik — mahsulot yaratish uchun majburiy bog'liqliklar.</summary>
    private static async Task<(Guid CategoryId, Guid UnitId)> CatalogAsync(WmsTenantScope scope)
    {
        Category category = await TestData.AddCategoryAsync(scope.Db);
        Unit unit = await scope.Db.Units.FirstAsync(TestContext.Current.CancellationToken);
        return (category.Id, unit.Id);
    }
}
