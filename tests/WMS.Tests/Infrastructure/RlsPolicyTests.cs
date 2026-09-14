using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WMS.Tests.Infrastructure;

/// <summary>
/// RLS himoyasining O'ZI joyidami — rol darajasida ham, jadval darajasida ham.
/// </summary>
/// <remarks>
/// <para>
/// Darvoza (2026-09-14). <see cref="RlsIsolationTests"/> «boshqa tenant ko'rinmadi»
/// degan XULOSANI o'lchaydi, bu yerdagi testlar esa o'sha xulosa NIMAGA tayanishini
/// o'lchaydi: <c>app_user</c> da <c>BYPASSRLS</c> yo'qligi va har tenant jadvalida
/// <c>tenant_isolation</c> siyosati <c>FORCE</c> bilan yoqilganligi.
/// </para>
/// <para>
/// Nega alohida kerak: agar kimdir ish vaqti roliga <c>BYPASSRLS</c> bersa yoki
/// migratsiya generatori siyosat yozishni to'xtatsa, izolyatsiya testlari HAMON
/// yashil qolishi mumkin — ularni EF ning global filtri ham o'tkazib yuboradi.
/// Bu yerdagi ikki da'vo esa darhol qizil beradi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class RlsPolicyTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public RlsPolicyTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ish_vaqti_roli_RLS_ni_chetlab_otolmaydi()
    {
        await using NpgsqlConnection connection = new(_fixture.RuntimeConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using NpgsqlCommand command = new(
            "SELECT rolbypassrls, rolsuper FROM pg_roles WHERE rolname = current_user;", connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        reader.GetBoolean(0).ShouldBeFalse();   // BYPASSRLS — YO'Q
        reader.GetBoolean(1).ShouldBeFalse();   // superuser ham emas (u ham siyosatni chetlab o'tardi)
    }

    [Theory]
    [InlineData("product")]
    [InlineData("warehouse_stock")]
    [InlineData("transfer")]
    [InlineData("batch")]
    [InlineData("counterparty")]
    public async Task Tenant_jadvalida_siyosat_FORCE_bilan_yoqilgan(string table)
    {
        await using NpgsqlConnection connection = new(_fixture.RuntimeConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        // `relforcerowsecurity` — jadval EGASIGA ham siyosat qo'llanishi (migratsiya roli
        // o'z jadvalidan hamma tenantni o'qib ketmasin).
        await using NpgsqlCommand command = new(
            """
            SELECT c.relrowsecurity, c.relforcerowsecurity,
                   (SELECT count(*) FROM pg_policy p WHERE p.polrelid = c.oid AND p.polname = 'tenant_isolation')
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'wms' AND c.relname = @table;
            """,
            connection);
        command.Parameters.AddWithValue("table", table);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).ShouldBeTrue($"wms.{table} jadvali topilmadi");
        reader.GetBoolean(0).ShouldBeTrue($"wms.{table}: RLS yoqilmagan");
        reader.GetBoolean(1).ShouldBeTrue($"wms.{table}: FORCE yo'q");
        reader.GetInt64(2).ShouldBe(1, $"wms.{table}: tenant_isolation siyosati yo'q");
    }
}
