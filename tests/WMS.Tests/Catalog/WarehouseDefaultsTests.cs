using WMS.Application.Common;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Catalog;

/// <summary>
/// Standart ombor (P2.6): forma omborni qachon SO'RAMAYDI va kimning tanlovi ustun keladi.
/// </summary>
/// <remarks>
/// <para>
/// Qabul mezoni «bitta omborli tenantda forma omborni so'ramaydi» — shuning uchun yagona
/// ombor SOZLAMASIZ ham amaldagi bo'lishi tekshiriladi: aks holda har mijoz ishni sozlamadan
/// boshlashi kerak bo'lardi.
/// </para>
/// <para>
/// ⚠️ Eng muhim da'vo — begona tenantning ombori. <c>tenant.default_*_warehouse_id</c> da FK
/// ATAYLAB yo'q (platforma jadvali ↔ tenant jadvali), ya'ni noto'g'ri qiymatni bazadan hech
/// kim ushlab qolmaydi: tekshiruv FAQAT servisda.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class WarehouseDefaultsTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public WarehouseDefaultsTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Yagona_ombor_sozlamasiz_ham_amalda_boladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse only = await TestData.AddWarehouseAsync(scope.Db, "Yagona");

        WarehouseDefaultsDto defaults = await scope.Service<IWarehouseService>()
            .GetDefaultsAsync(TestContext.Current.CancellationToken);

        // Sozlama BO'SH, lekin forma baribir omborni so'ramaydi.
        defaults.RawWarehouseId.ShouldBeNull();
        defaults.FinishedWarehouseId.ShouldBeNull();
        defaults.EffectiveRawId.ShouldBe(only.Id);
        defaults.EffectiveFinishedId.ShouldBe(only.Id);
    }

    [Fact]
    public async Task Ikki_ombor_bolsa_sozlama_ishlatiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse raw = await TestData.AddWarehouseAsync(scope.Db, "Xomashyo", WarehouseType.Raw);
        Warehouse finished = await TestData.AddWarehouseAsync(scope.Db, "Tayyor", WarehouseType.Finished);

        IWarehouseService warehouses = scope.Service<IWarehouseService>();

        // Ikkita ombor bor — sozlamasiz forma so'raydi.
        WarehouseDefaultsDto before = await warehouses.GetDefaultsAsync(TestContext.Current.CancellationToken);
        before.EffectiveRawId.ShouldBeNull();
        before.EffectiveFinishedId.ShouldBeNull();

        await warehouses.SetDefaultsAsync(
            new WarehouseDefaultsDto { RawWarehouseId = raw.Id, FinishedWarehouseId = finished.Id },
            TestContext.Current.CancellationToken);

        WarehouseDefaultsDto after = await warehouses.GetDefaultsAsync(TestContext.Current.CancellationToken);
        after.RawWarehouseId.ShouldBe(raw.Id);
        after.FinishedWarehouseId.ShouldBe(finished.Id);
        after.EffectiveRawId.ShouldBe(raw.Id);
        after.EffectiveFinishedId.ShouldBe(finished.Id);
    }

    [Fact]
    public async Task Xodim_tanlovi_tenant_sozlamasidan_ustun()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        Warehouse raw = await TestData.AddWarehouseAsync(scope.Db, "Xomashyo", WarehouseType.Raw);
        Warehouse finished = await TestData.AddWarehouseAsync(scope.Db, "Tayyor", WarehouseType.Finished);
        Warehouse mine = await TestData.AddWarehouseAsync(scope.Db, "Sex ombori", WarehouseType.Finished);
        UserProfile profile = await TestData.AddUserAsync(scope.Db, "Sex xodimi");

        IWarehouseService warehouses = scope.Service<IWarehouseService>();
        await warehouses.SetDefaultsAsync(
            new WarehouseDefaultsDto { RawWarehouseId = raw.Id, FinishedWarehouseId = finished.Id },
            TestContext.Current.CancellationToken);

        // Shaxsiy tanlov — oddiy autentifikatsiya yetadi (o'z profili), ruxsat kerak emas.
        scope.AsUser(profile.Id);
        await warehouses.SetMyDefaultWarehouseAsync(mine.Id, TestContext.Current.CancellationToken);

        WarehouseDefaultsDto defaults = await warehouses.GetDefaultsAsync(TestContext.Current.CancellationToken);
        defaults.UserWarehouseId.ShouldBe(mine.Id);
        defaults.RawWarehouseId.ShouldBe(raw.Id);
        defaults.EffectiveRawId.ShouldBe(mine.Id);
        defaults.EffectiveFinishedId.ShouldBe(mine.Id);

        // Tanlov olib tashlansa — tenant sozlamasi qaytadi.
        await warehouses.SetMyDefaultWarehouseAsync(null, TestContext.Current.CancellationToken);
        WarehouseDefaultsDto cleared = await warehouses.GetDefaultsAsync(TestContext.Current.CancellationToken);
        cleared.UserWarehouseId.ShouldBeNull();
        cleared.EffectiveRawId.ShouldBe(raw.Id);
        cleared.EffectiveFinishedId.ShouldBe(finished.Id);
    }

    [Fact]
    public async Task Begona_tenant_ombori_saqlanmaydi()
    {
        TestTenant other = await _fixture.CreateTenantAsync();
        Guid foreignWarehouseId;
        await using (WmsTenantScope otherScope = _fixture.BeginScope(other))
        {
            foreignWarehouseId = (await TestData.AddWarehouseAsync(otherScope.Db, "Begona")).Id;
        }

        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        Warehouse own = await TestData.AddWarehouseAsync(scope.Db, "O'ziniki");
        UserProfile profile = await TestData.AddUserAsync(scope.Db);

        IWarehouseService warehouses = scope.Service<IWarehouseService>();

        await Should.ThrowAsync<NotFoundException>(async () =>
            await warehouses.SetDefaultsAsync(
                new WarehouseDefaultsDto { RawWarehouseId = foreignWarehouseId },
                TestContext.Current.CancellationToken));

        scope.AsUser(profile.Id);
        await Should.ThrowAsync<NotFoundException>(async () =>
            await warehouses.SetMyDefaultWarehouseAsync(foreignWarehouseId, TestContext.Current.CancellationToken));

        // O'chirilgan ombor ham qabul qilinmaydi.
        await warehouses.DeleteAsync(own.Id);
        await Should.ThrowAsync<NotFoundException>(async () =>
            await warehouses.SetDefaultsAsync(
                new WarehouseDefaultsDto { FinishedWarehouseId = own.Id },
                TestContext.Current.CancellationToken));
    }
}
