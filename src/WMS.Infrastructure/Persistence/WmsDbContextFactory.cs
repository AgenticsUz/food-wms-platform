using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using WMS.Infrastructure.Persistence.Rls;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// FAQAT <c>dotnet ef migrations add</c> uchun (design-time). Ulanish satri
/// <c>WMS_DESIGN_CONNECTION</c> muhitidan, bo'lmasa dev compose'dagi app_migrator.
/// Migratsiya YARATISH bazaga ulanmaydi — satr faqat provayder tanlovi uchun.
/// </summary>
public sealed class WmsDbContextFactory : IDesignTimeDbContextFactory<WmsDbContext>
{
    private const string DefaultConnection =
        "Host=localhost;Port=5434;Database=agentics_wms;Username=app_migrator;Password=Dev_AppMigrator_2026!;Search Path=wms,public";

    public WmsDbContext CreateDbContext(string[] args)
    {
        string connection = Environment.GetEnvironmentVariable("WMS_DESIGN_CONNECTION") ?? DefaultConnection;

        DbContextOptions<WmsDbContext> options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable(WmsDbContext.MigrationsHistoryTable, WmsDbContext.DefaultSchema))
            .ReplaceService<IMigrationsSqlGenerator, WmsMigrationsSqlGenerator>()
            .Options;

        return new WmsDbContext(options, new CurrentTenant());
    }
}
