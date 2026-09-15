using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Access;
using WMS.Infrastructure.Common;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Persistence.Rls;
using WMS.Infrastructure.Seeding;
using WMS.Infrastructure.Tenancy;
using WmsCurrentTenant = WMS.Infrastructure.Tenancy.CurrentTenant;

namespace WMS.Infrastructure.DependencyInjection;

public static class WmsInfrastructureExtensions
{
    /// <summary>
    /// Baza (Postgres + RLS), tenant konteksti, huquqlar manbai, obuna holati, bazaviy seed.
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>ConnectionStrings:wms</c> sozlanmagan bo'lsa xost KO'TARILMAYDI (fail-closed): SQLite
    /// davrida ulanish satri bo'lmasa ilova yonida yangi bo'sh <c>wms.db</c> yaratib, jimgina
    /// bo'sh ma'lumot bilan ishlab ketardi.
    /// </remarks>
    public static IHostApplicationBuilder AddWmsInfrastructure(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IConfiguration configuration = builder.Configuration;
        IServiceCollection services = builder.Services;

        string runtime = configuration.GetConnectionString(WmsDatabaseConnections.RuntimeConnectionName) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"'ConnectionStrings:{WmsDatabaseConnections.RuntimeConnectionName}' sozlanmagan (app_user ulanishi).");

        string? privileged = configuration.GetConnectionString(WmsDatabaseConnections.PrivilegedConnectionName);
        string runtimeUser = new NpgsqlConnectionStringBuilder(runtime).Username ?? "app_user";

        services.AddSingleton(new WmsDatabaseConnections(privileged, runtime, runtimeUser, EnsureRuntimeGrants: true));

        // Scoped tenant konteksti (paketning AsyncLocal nusxasi EMAS — sababi CurrentTenant izohida).
        services.AddScoped<WmsCurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<WmsCurrentTenant>());
        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<WmsDbContext>((sp, options) => options
            .UseNpgsql(runtime, npgsql => npgsql.MigrationsHistoryTable(WmsDbContext.MigrationsHistoryTable, WmsDbContext.DefaultSchema))
            .ReplaceService<IMigrationsSqlGenerator, WmsMigrationsSqlGenerator>()
            .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddMemoryCache();
        services.Configure<SubscriptionOptions>(configuration.GetSection(SubscriptionOptions.SectionName));
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));

        // AI (F10·A0). Kalit bo'sh bo'lsa ham bog'lanadi: modul «o'chiq» javobini shu
        // sozlamalardan o'qiydi (`AiOptions.IsConfigured`).
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddScoped<ITenantStateService, TenantStateService>();

        // Hujjat raqamlari (P2.4) — infratuzilma servisi: modul emas, jadval bilan bitta.
        services.AddScoped<DocumentNumbers>();
        services.AddScoped<IDocumentNumbers>(sp => sp.GetRequiredService<DocumentNumbers>());
        services.AddScoped<IRequestWarnings, RequestWarnings>();

        // Huquqlar manbai IKKI interfeysda bitta nusxa: paketning JIT middleware'i
        // `IsAuthoritative` ni ko'rib ruxsatlarni xaritadan YOZMAYDI (WmsAccessResolver izohi).
        services.AddScoped<WmsAccessResolver>();
        services.AddScoped<IWmsAccessResolver>(sp => sp.GetRequiredService<WmsAccessResolver>());
        services.AddScoped<IEffectiveAccessResolver>(sp => sp.GetRequiredService<WmsAccessResolver>());
        services.AddScoped<WmsAccessContext>();

        services.AddScoped<TenantBaseline>();
        services.AddScoped<BaseCatalogSeeder>();
        services.AddScoped<DemoSeeder>();

        services.AddHealthChecks().AddNpgSql(runtime, name: "postgres", tags: ["ready"]);

        return builder;
    }
}
