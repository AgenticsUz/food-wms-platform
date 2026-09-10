using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Platform.Web.Proxy;
using Platform.Web.UserSync;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using WMS.API.Authentication;
using WMS.API.Authorization;
using WMS.API.Hosting;
using WMS.API.Identity;
using WMS.API.Middleware;
using WMS.Application.Interfaces;
using WMS.Infrastructure.DependencyInjection;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Seeding;

QuestPDF.Settings.License = LicenseType.Community;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // SQLite davridagi kunlik fayl logi (`logs/wms-.log`) O'CHDI: konteynerda log — stdout.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture));

    // --- qatlamlar ---
    builder.AddWmsInfrastructure();
    builder.Services.AddWmsModules();

    // --- auth: Identity tokeni, ikki yuza ---
    builder.Services.AddWmsAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddWmsAuthorization();

    // JIT: tokendagi tenant va foydalanuvchini WMS bazasiga NUSXALAYDI (§3.7). Usiz Console'da
    // zavodga biriktirilgan odam birinchi so'rovida 403 olardi.
    builder.Services.AddPlatformUserSync<WmsPlatformUserSink>(WmsRoleMap.Declare());

    builder.Services.AddScoped<IRequestLanguage, RequestLanguage>();
    builder.Services.AddWmsCors(builder.Configuration);
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<AuditLogFilter>();
        options.Filters.Add<ResponseLocalizationFilter>();
    });
    builder.Services.AddOpenApi();

    WebApplication app = builder.Build();

    // `migrate` / `seed demo` — sxema + bazaviy katalog va CHIQISH (compose `wms-migrator`).
    int? exitCode = await MigrationStartup.RunAsync(
        app,
        args,
        afterMigrateAsync: async (services, ct) =>
        {
            await services.GetRequiredService<BaseCatalogSeeder>().SeedAsync(ct);

            // Demo tenant — FAQAT `seed demo` argumenti bilan (prod bazasiga demo yozilmasin).
            // Bazaviy katalogdan KEYIN: demo `enterprise` planiga tayanadi.
            if (MigrationStartup.WantsDemoSeed(args))
            {
                await services.GetRequiredService<DemoSeeder>().SeedAsync(ct);
            }
        },
        cancellationToken: app.Lifetime.ApplicationStopping);
    if (exitCode is not null)
    {
        return exitCode.Value;
    }

    // Logolar wwwroot'da; papka startupda bo'lmasa static file'lar keyingi restartgacha 404 berardi.
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "tenants"));

    // --- middleware zanjiri ---
    // ⚠️ Forwarded headers ENG BOSHIDA: keyingi hamma narsa haqiqiy sxema, xost va mijoz IP'sini ko'rsin.
    app.UseProxyForwardedHeaders();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    // Logolar anonim beriladi — ular maxfiy emas va brendlash sahifa tokeni paydo bo'lishidan oldin ham kerak.
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors(WmsCors.PolicyName);
    app.UseAuthentication();

    // ⚠️ TARTIB SHART: JIT autentifikatsiyadan KEYIN va tenant hal qilinishidan OLDIN — sink
    // `wms.tenant` qatorini o'zi yozadi; huquqlar esa aynan sink yozgan profil va roldan o'qiladi.
    app.UsePlatformUserSync();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseMiddleware<SubscriptionEnforcementMiddleware>();
    app.UseMiddleware<EffectiveAccessMiddleware>();
    app.UseAuthorization();

    // --- infra endpoint'lar ---
    app.MapGet("/alive", () => Results.Ok(new { status = "alive" })).ExcludeFromDescription();
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => !check.Tags.Contains("ready") });
    app.MapHealthChecks("/health/ready");

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/scalar", options => options.WithTitle("Agentics WMS API"));
    }

    // Ikki yuza: `/api/*` (BaseController) va `/admin/v1/*` (AdminBaseController) — marshrutlar
    // controller atributlarida, chegara esa audience va siyosatda (WmsSurfaces).
    app.MapControllers();

    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not OperationCanceledException and not HostAbortedException)
{
    Log.Fatal(ex, "wms-api ishga tushmadi");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
