using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WMS.Domain.Common;
using WMS.Infrastructure.Persistence;

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

    [Fact]
    public async Task HAR_tenant_entity_jadvali_FORCE_bilan_qoriqlanadi()
    {
        // ⚠️ Ro'yxat QO'LDA sanalmaydi — u EF MODELIDAN olinadi: mezon «jadvalda tenant_id
        // ustuni bor» EMAS, «entity `ITenantEntity` dan meros olgan». Farq muhim:
        // `telegram_outbox`, `telegram_chat_state`, `telegram_group`, `telegram_link_token`
        // da ham `tenant_id` ustuni bor, lekin ular ATAYLAB platforma jadvallari (bot chati
        // bitta tenant ichida yashamaydi — `WmsDbContext` izohi). Ustun bo'yicha tekshirish
        // ularni «buzilgan» deb ko'rsatib, darvozani shovqinga aylantirardi.
        //
        // Nima ushlanadi: migratsiya backfill'i vaqtincha `NO FORCE` qilib QAYTARISHNI
        // unutsa (2026-09-15 da aynan shunday yozilib, lokal stendda topildi) yoki yangi
        // tenant jadvali RLS'siz qo'shilsa.
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        string[] tenantTables = [.. db.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.GetTableName())
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct()
            .Order()];

        tenantTables.Length.ShouldBeGreaterThan(20, "tenant entity'lari topilmadi — model o'qilmadi");

        await using NpgsqlConnection connection = new(_fixture.RuntimeConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using NpgsqlCommand command = new(
            """
            SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity,
                   (SELECT count(*) FROM pg_policy p WHERE p.polrelid = c.oid AND p.polname = 'tenant_isolation')
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'wms' AND c.relkind = 'r' AND c.relname = ANY(@tables)
            ORDER BY c.relname;
            """,
            connection);
        command.Parameters.AddWithValue("tables", tenantTables);

        List<string> broken = [];
        int seen = 0;

        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken))
        {
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                seen++;
                string table = reader.GetString(0);
                if (!reader.GetBoolean(1)) { broken.Add($"{table}: RLS yoqilmagan"); }
                if (!reader.GetBoolean(2)) { broken.Add($"{table}: FORCE yo'q"); }
                if (reader.GetInt64(3) != 1) { broken.Add($"{table}: tenant_isolation siyosati yo'q"); }
            }
        }

        broken.ShouldBeEmpty();

        // Modeldagi har jadval bazada ham TOPILISHI shart — topilmasa tekshiruv jimgina
        // bo'shab qolardi (migratsiya tushib qolgan holat).
        seen.ShouldBe(tenantTables.Length);
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
