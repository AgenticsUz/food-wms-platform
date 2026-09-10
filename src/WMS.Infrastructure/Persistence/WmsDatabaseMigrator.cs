using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WMS.Infrastructure.Persistence.Rls;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Ikki ulanish satri (ikki rol modeli): <see cref="Privileged"/> — <c>app_migrator</c> (DDL),
/// <see cref="Runtime"/> — <c>app_user</c> (kundalik so'rovlar, RLS majburlanadi).
/// </summary>
public sealed record WmsDatabaseConnections(string? Privileged, string? Runtime, string RuntimeUsername, bool EnsureRuntimeGrants)
{
    public const string RuntimeConnectionName = "wms";
    public const string PrivilegedConnectionName = "wms_migrator";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Runtime);
    public bool CanMigrate => !string.IsNullOrWhiteSpace(Privileged);
}

/// <summary>
/// Sxemani IMTIYOZLI ulanish bilan yangilaydi, so'ng ish vaqti roliga huquq beradi.
/// </summary>
/// <remarks>
/// DI'dagi <see cref="WmsDbContext"/> <c>app_user</c> bilan ulanadi (DDL qila olmaydi) —
/// shuning uchun migratsiya uchun alohida qisqa muddatli kontekst yasaladi.
/// </remarks>
public static partial class WmsDatabaseMigrator
{
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex RoleNamePattern();

    /// <summary>Idempotent GRANT + audit_log REVOKE (tartib muhim: REVOKE grantdan KEYIN).</summary>
    private const string EnsureGrantsSqlTemplate = """
        DO
        $do$
        DECLARE
            runtime_role text := '__RUNTIME_ROLE__';
            owner_role   text := current_user;
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = runtime_role) THEN
                RAISE NOTICE 'Ish vaqti roli % topilmadi - GRANT o''tkazib yuborildi.', runtime_role;
                RETURN;
            END IF;

            EXECUTE format('GRANT USAGE ON SCHEMA wms TO %I', runtime_role);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA wms TO %I', runtime_role);
            EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA wms TO %I', runtime_role);
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA wms GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I', owner_role, runtime_role);
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA wms GRANT USAGE, SELECT ON SEQUENCES TO %I', owner_role, runtime_role);

            -- wms.audit_log — APPEND-ONLY. GRANT'dan KEYIN turishi SHART.
            IF to_regclass('wms.audit_log') IS NOT NULL THEN
                EXECUTE format('REVOKE UPDATE, DELETE, TRUNCATE ON wms.audit_log FROM %I', runtime_role);
            END IF;

            -- Migratsiya tarixi — ilova roliga umuman kerak emas.
            IF to_regclass('wms.__ef_migrations_history') IS NOT NULL THEN
                EXECUTE format('REVOKE ALL ON wms.__ef_migrations_history FROM %I', runtime_role);
            END IF;
        END
        $do$;
        """;

    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        WmsDatabaseConnections connections = services.GetRequiredService<WmsDatabaseConnections>();
        if (!connections.CanMigrate)
        {
            throw new InvalidOperationException(
                $"Migratsiya ulanishi berilmagan: 'ConnectionStrings:{WmsDatabaseConnections.PrivilegedConnectionName}'.");
        }

        await using WmsDbContext context = CreatePrivilegedContext(services, connections.Privileged!);
        await context.Database.MigrateAsync(cancellationToken);

        if (connections.EnsureRuntimeGrants)
        {
            if (!RoleNamePattern().IsMatch(connections.RuntimeUsername))
            {
                throw new InvalidOperationException($"Postgres rol nomiga o'xshamaydi: '{connections.RuntimeUsername}'.");
            }

            string sql = EnsureGrantsSqlTemplate.Replace("__RUNTIME_ROLE__", connections.RuntimeUsername, StringComparison.Ordinal);
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    /// <summary>Imtiyozli kontekst — interceptor'larsiz (migratsiyada tenant kerak emas).</summary>
    public static WmsDbContext CreatePrivilegedContext(IServiceProvider services, string connectionString)
    {
        DbContextOptions<WmsDbContext> options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(WmsDbContext.MigrationsHistoryTable, WmsDbContext.DefaultSchema))
            .ReplaceService<IMigrationsSqlGenerator, WmsMigrationsSqlGenerator>()
            .UseLoggerFactory(services.GetRequiredService<ILoggerFactory>())
            .Options;

        return new WmsDbContext(options, new CurrentTenant());
    }
}
