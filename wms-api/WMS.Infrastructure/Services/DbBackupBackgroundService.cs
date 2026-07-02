using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>
/// SQLite bazasidan kunlik izchil snapshot oladi (VACUUM INTO — WAL bilan xavfsiz),
/// timestamped fayl sifatida saqlaydi, eski nusxalarni tozalaydi.
/// Sozlamalar (appsettings "Backup"): Enabled, Directory, IntervalHours, Retention.
/// Off-site (S3/rsync) — deploy skriptida yoki bu yerga hook qo'shib kengaytiriladi.
/// </summary>
public class DbBackupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DbBackupBackgroundService> _logger;

    public DbBackupBackgroundService(IServiceScopeFactory scopeFactory,
        IConfiguration config, ILogger<DbBackupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Backup:Enabled", true)) return;

        var directory = _config.GetValue("Backup:Directory", "backups")!;
        var intervalHours = _config.GetValue("Backup:IntervalHours", 24);
        var retention = _config.GetValue("Backup:Retention", 14);

        // Startup'da migratsiya/seed tugashini kutamiz
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunBackupAsync(directory, retention, stoppingToken);
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task RunBackupAsync(string directory, int retention, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

            // Faqat SQLite uchun — PostgreSQL'ga o'tilsa pg_dump ishlatiladi
            if (!db.Database.IsSqlite())
            {
                _logger.LogInformation("DB backup: SQLite emas, o'tkazib yuborildi");
                return;
            }

            Directory.CreateDirectory(directory);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var target = Path.Combine(directory, $"wms-{stamp}.db");
            var escaped = target.Replace("'", "''");

            var conn = (SqliteConnection)db.Database.GetDbConnection();
            var opened = conn.State != System.Data.ConnectionState.Open;
            if (opened) await conn.OpenAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"VACUUM INTO '{escaped}';";
                await cmd.ExecuteNonQueryAsync(ct);
            }
            finally
            {
                if (opened) await conn.CloseAsync();
            }

            CleanupOld(directory, retention);
            _logger.LogInformation("DB backup yaratildi: {Target}", target);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB backup xatosi");
        }
    }

    private void CleanupOld(string directory, int retention)
    {
        var files = new DirectoryInfo(directory)
            .GetFiles("wms-*.db")
            .OrderByDescending(f => f.Name)
            .Skip(retention)
            .ToList();
        foreach (var f in files)
        {
            try { f.Delete(); } catch { /* keyingi safar */ }
        }
    }
}
