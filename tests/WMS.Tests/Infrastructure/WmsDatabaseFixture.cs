using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using WMS.API.Hosting;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.DependencyInjection;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Seeding;
using WMS.Infrastructure.Tenancy;

namespace WMS.Tests.Infrastructure;

/// <summary>Test tenanti — id va kodi (RLS konteksti shu ikkisidan qo'yiladi).</summary>
/// <param name="Id">Tenant identifikatori.</param>
/// <param name="Code">Tenant kodi.</param>
public sealed record TestTenant(Guid Id, string Code);

/// <summary>
/// HAQIQIY Postgres konteyneri va TO'LIQ DI grafi (<c>AddWmsInfrastructure</c> +
/// <c>AddWmsModules</c>) — servislar prod'dagi nusxasi bilan bir xil ulanadi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nega InMemory emas.</b> Tekshiriladigan da'volar bazaning o'ziga tayanadi:
/// RLS siyosati (<c>tenant_isolation</c>), <c>xmin</c> optimistik versiya (parallel
/// tasdiqda 409), filtrli noyob indeks (komissiya bir marta) va <c>numeric(18,3)</c>
/// yaxlitlashi. InMemory provayderida bularning birortasi ham yo'q.
/// </para>
/// <para>
/// ⚠️ <b>Rollar va kengaytmalar QO'LDA yaratiladi.</b> Prod'da ularni
/// <c>docker/postgres/init/01-roles-schemas.sql</c> qo'yadi, Testcontainers'da esa
/// init skriptlari yurmaydi. Eng muhimi — <c>app_user</c> ning <c>NOBYPASSRLS</c>
/// bo'lishi: superuser bilan ulansak RLS umuman ishlamaydi va tenant izolyatsiyasi
/// testlari hech narsa o'lchamasdi.
/// </para>
/// </remarks>
public sealed class WmsDatabaseFixture : IAsyncLifetime
{
    /// <summary>Konteyner obrazi — prod spetsifikatsiyasidagi versiya bilan bir xil.</summary>
    public const string Image = "postgres:17-alpine";

    private const string DatabaseName = "agentics_wms";
    private const string RuntimeRole = "app_user";
    private const string MigratorRole = "app_migrator";
    private const string RolePassword = "Test_Role_2026!";

    private PostgreSqlContainer? _container;
    private IHost? _host;

    /// <summary>Ilova DI grafi (host ishga TUSHIRILMAYDI — fon xizmatlari yurmasin).</summary>
    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("Fixture hali ishga tushmagan.");

    /// <summary>Ish vaqti (<c>app_user</c>) ulanish satri — RLS majburlanadigan yo'l.</summary>
    public string RuntimeConnectionString => ConnectionFor(RuntimeRole);

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder(Image)
            .WithDatabase(DatabaseName)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
        await PrepareDatabaseAsync();

        _host = BuildHost();
        await WmsDatabaseMigrator.MigrateAsync(_host.Services);

        // Feature katalogi va planlar — RLS'siz platforma jadvallari, bir marta.
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BaseCatalogSeeder>().SeedAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _host?.Dispose();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Yangi test tenanti: <c>wms.tenant</c> qatori (barcha modullar yoqilgan) va
    /// <see cref="TenantBaseline"/> (tizim rollari, birliklar).
    /// </summary>
    /// <param name="code">Tenant kodi; berilmasa noyob kod yasaladi.</param>
    /// <returns>Yaratilgan tenant.</returns>
    /// <remarks>
    /// Har test O'Z tenantini oladi — shuning uchun testlar orasida <c>TRUNCATE</c>
    /// kerak emas: qolgan hamma jadval tenant jadvali va RLS ularni baribir ajratadi.
    /// Plan ATAYLAB biriktirilmaydi: plansiz tenantda limitlar cheksiz
    /// (<c>PlanLimits</c>), ya'ni transfer testlari limitga urilib qolmaydi.
    /// </remarks>
    public async Task<TestTenant> CreateTenantAsync(string? code = null)
    {
        Guid tenantId = Guid.CreateVersion7();

        // ⚠️ Kod TASODIFIY v4 dan olinadi, `tenantId` (v7) dan EMAS: v7 ning bosh belgilari —
        // vaqt tamg'asi, ya'ni bir vaqtda tenant yaratgan ikki test bir xil kod olib
        // `tenant.code` noyobligini buzardi (23505).
        string tenantCode = code ?? $"t-{Guid.NewGuid().ToString("N")[..12]}";

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Code = tenantCode,
            Name = tenantCode,
            Modules = string.Join(' ', ModuleCodes.All),
            IsActive = true,
            SubscriptionStatus = SubscriptionStatus.Active,
            SyncedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<TenantBaseline>().EnsureAsync(tenantId, tenantCode);
        return new TestTenant(tenantId, tenantCode);
    }

    /// <summary>Tenant konteksti o'rnatilgan yangi DI qamrovi.</summary>
    /// <param name="tenant">Tenant.</param>
    /// <returns>Qamrov.</returns>
    public WmsTenantScope BeginScope(TestTenant tenant) => new(Services, tenant);

    private string ConnectionFor(string role) =>
        new NpgsqlConnectionStringBuilder(_container!.GetConnectionString())
        {
            Username = role,
            Password = RolePassword,
            SearchPath = "wms,public",
            IncludeErrorDetail = true,
        }.ConnectionString;

    private IHost BuildHost()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(
            new HostApplicationBuilderSettings { ApplicationName = "WMS.Tests" });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{WmsDatabaseConnections.RuntimeConnectionName}"] = ConnectionFor(RuntimeRole),
            [$"ConnectionStrings:{WmsDatabaseConnections.PrivilegedConnectionName}"] = ConnectionFor(MigratorRole),
        });

        builder.Services.AddLogging();
        builder.AddWmsInfrastructure();
        builder.Services.AddWmsModules();

        // Program.cs da so'rov qamrovidan keladi; testda til doim o'zbekcha.
        builder.Services.AddScoped<IRequestLanguage, TestRequestLanguage>();

        return builder.Build();
    }

    /// <summary>Kengaytmalar, ikki rol va <c>wms</c> sxemasi — init skriptining test nusxasi.</summary>
    private async Task PrepareDatabaseAsync()
    {
        string sql = $"""
            CREATE EXTENSION IF NOT EXISTS pgcrypto;
            CREATE EXTENSION IF NOT EXISTS citext;
            CREATE EXTENSION IF NOT EXISTS pg_trgm;

            DO $do$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{MigratorRole}') THEN
                    CREATE ROLE {MigratorRole} LOGIN PASSWORD '{RolePassword}'
                        NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION INHERIT;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{RuntimeRole}') THEN
                    CREATE ROLE {RuntimeRole} LOGIN PASSWORD '{RolePassword}'
                        NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT;
                END IF;
            END
            $do$;

            CREATE SCHEMA IF NOT EXISTS wms AUTHORIZATION {MigratorRole};
            GRANT CONNECT ON DATABASE {DatabaseName} TO {RuntimeRole}, {MigratorRole};
            GRANT USAGE ON SCHEMA wms TO {RuntimeRole};
            GRANT USAGE, CREATE ON SCHEMA wms TO {MigratorRole};
            """;

        await using NpgsqlConnection connection = new(_container!.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>Konteyner bir marta ko'tarilsin — barcha testlar uni bo'lishadi.</summary>
[CollectionDefinition(Name)]
public sealed class WmsDatabaseCollection : ICollectionFixture<WmsDatabaseFixture>
{
    /// <summary>To'plam nomi.</summary>
    public const string Name = "wms-database";
}

/// <summary>Testda so'rov tili — doim o'zbekcha (xato matnlari tarjimasi tekshirilsin).</summary>
internal sealed class TestRequestLanguage : IRequestLanguage
{
    /// <inheritdoc />
    public string Current => "uz";
}
