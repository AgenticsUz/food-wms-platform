using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Tests.Infrastructure;

/// <summary>
/// Tenant izolyatsiyasi BAZA darajasida: EF global filtri emas, RLS siyosati
/// tekshiriladi — shuning uchun so'rovlar <c>IgnoreQueryFilters()</c> bilan yuradi.
/// </summary>
/// <remarks>
/// Darvoza (2026-09-14): bu ikki test <c>app_user</c> rolidan <c>NOBYPASSRLS</c> olib
/// tashlansa yoki migratsiya generatori siyosat yozishni to'xtatsa DARHOL qizil beradi.
/// Ularsiz «tenant ko'rinmaydi» degan da'voni faqat C# kodi tasdiqlardi, holbuki
/// prod'da himoyaning oxirgi qatlami — Postgres.
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class RlsIsolationTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public RlsIsolationTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Boshqa_tenant_qatorlari_RLS_bilan_yashiriladi()
    {
        TestTenant first = await _fixture.CreateTenantAsync();
        TestTenant second = await _fixture.CreateTenantAsync();

        await using (WmsTenantScope scope = _fixture.BeginScope(first))
        {
            await TestData.AddProductAsync(scope.Db, "Birinchi tenant mahsuloti");
        }

        await using WmsTenantScope other = _fixture.BeginScope(second);

        // Global filtr o'chirilgan: qatorni yashirayotgan narsa — AYNAN RLS siyosati.
        List<Product> visible = await other.Db.Products.IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);

        visible.ShouldBeEmpty();
    }

    [Fact]
    public async Task Tenant_konteksti_yoq_qamrov_hech_narsa_kormaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            await TestData.AddProductAsync(scope.Db, "Kontekstsiz ko'rinmasin");
        }

        // Tenant O'RNATILMAGAN qamrov: `app.tenant_id` bo'sh, siyosat NULL escape bermaydi.
        await using AsyncServiceScope bare = _fixture.Services.CreateAsyncScope();
        WmsDbContext db = bare.ServiceProvider.GetRequiredService<WmsDbContext>();

        int count = await db.Products.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken);

        count.ShouldBe(0);
    }
}
