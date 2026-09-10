using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WMS.Infrastructure.Persistence;

/// <summary>Migratsiya rejimi.</summary>
public enum MigrationMode
{
    /// <summary>Sukut: faqat Development'da, migrator ulanishi bo'lsa, startupda qo'llanadi.</summary>
    Auto = 0,

    /// <summary>Qo'llaydi va HTTP serverni ko'tarmasdan chiqadi (compose <c>wms-migrator</c>).</summary>
    RunAndExit = 1,

    /// <summary>Tegmaydi (prod'dagi ishlab turgan konteyner, <c>app_user</c>).</summary>
    None = 2,
}

/// <summary>
/// Migratsiya qadami — deploy shartnomasi (Wash/HRM naqshi).
/// </summary>
/// <remarks>
/// Bir martalik konteyner <c>migrate</c> argumenti bilan (yoki <c>Migrations__Mode=run-and-exit</c>)
/// <c>app_migrator</c> ulanishida sxemani yangilaydi, bazaviy katalogni seed qiladi va CHIQADI;
/// xato → exit 1 (compose zanjiri jimgina o'tib ketmasin). <c>seed demo</c> — demo tenant ham.
/// SQLite davrida <c>Database.Migrate()</c> har startupda ilova rolida yurardi — prod'da
/// ilova roliga DDL huquqi berilmaydi.
/// </remarks>
public static class MigrationStartup
{
    public const string ModeConfigurationKey = "Migrations:Mode";
    public const string MigrateArgument = "migrate";
    public const string SeedArgument = "seed";
    public const string DemoArgument = "demo";
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;

    public static bool WantsDemoSeed(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return Array.Exists(args, a => string.Equals(a, DemoArgument, StringComparison.OrdinalIgnoreCase));
    }

    public static MigrationMode ResolveMode(IConfiguration configuration, string[] args)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(args);

        if (Array.Exists(args, a => string.Equals(a, MigrateArgument, StringComparison.OrdinalIgnoreCase))
            || Array.Exists(args, a => string.Equals(a, SeedArgument, StringComparison.OrdinalIgnoreCase)))
        {
            return MigrationMode.RunAndExit;
        }

        string? value = configuration[ModeConfigurationKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return MigrationMode.Auto;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "auto" => MigrationMode.Auto,
            "run-and-exit" => MigrationMode.RunAndExit,
            "none" => MigrationMode.None,
            _ => throw new InvalidOperationException(
                $"'{ModeConfigurationKey}' qiymati tanilmadi: '{value}'. Ruxsat: auto, run-and-exit, none."),
        };
    }

    /// <summary>
    /// <see langword="null"/> — davom eting (server ko'tariladi); son — jarayon shu kod bilan tugasin.
    /// </summary>
    public static async Task<int?> RunAsync(
        IHost host,
        string[] args,
        Func<IServiceProvider, CancellationToken, Task>? afterMigrateAsync = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        IServiceProvider services = host.Services;
        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        IHostEnvironment environment = services.GetRequiredService<IHostEnvironment>();
        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MigrationStartup));
        WmsDatabaseConnections connections = services.GetRequiredService<WmsDatabaseConnections>();

        MigrationMode mode = ResolveMode(configuration, args);

        if (mode == MigrationMode.None)
        {
            return null;
        }

        if (mode == MigrationMode.Auto)
        {
            if (!environment.IsDevelopment() || !connections.CanMigrate)
            {
                return null;
            }

            await using AsyncServiceScope autoScope = services.CreateAsyncScope();
            await WmsDatabaseMigrator.MigrateAsync(autoScope.ServiceProvider, cancellationToken);
            if (afterMigrateAsync is not null)
            {
                await afterMigrateAsync(autoScope.ServiceProvider, cancellationToken);
            }

            return null;
        }

        if (!connections.CanMigrate)
        {
            logger.LogCritical("Migratsiya rejimi 'run-and-exit', lekin 'ConnectionStrings:{Name}' berilmagan.", WmsDatabaseConnections.PrivilegedConnectionName);
            return FailureExitCode;
        }

        try
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();
            await WmsDatabaseMigrator.MigrateAsync(scope.ServiceProvider, cancellationToken);
            if (afterMigrateAsync is not null)
            {
                await afterMigrateAsync(scope.ServiceProvider, cancellationToken);
            }
        }
#pragma warning disable CA1031 // Bir martalik qadam: HAR QANDAY nosozlik exit kodiga aylanadi.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogCritical(exception, "Migratsiya yiqildi — jarayon {ExitCode} bilan tugaydi.", FailureExitCode);
            return FailureExitCode;
        }

        logger.LogInformation("Migratsiya qo'llandi — HTTP server ko'tarilmaydi, jarayon {ExitCode} bilan tugaydi.", SuccessExitCode);
        return SuccessExitCode;
    }
}
