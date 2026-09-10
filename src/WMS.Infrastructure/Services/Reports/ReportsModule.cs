using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Reports;

/// <summary>W1·6 — hisobotlar: analitika, Excel eksport/import, valyuta kurslari.</summary>
/// <remarks>
/// <see cref="ReportBranding"/> statik — DI'da yo'q; uni transfer va yuk xati PDF'lari ham chaqiradi.
/// </remarks>
public static class ReportsModule
{
    /// <summary>Markaziy bank (cbu.uz) kurslari uchun nomli HTTP mijoz.</summary>
    public const string CurrencyHttpClient = "cbu";

    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IImportService, ImportService>();

        // Tashqi API osilib qolsa so'rov ham osilmasin: 10 s, keyin controller keshdagi
        // kursni yoki 503 ni qaytaradi (yumshoq yiqilish).
        services.AddHttpClient(CurrencyHttpClient, client =>
        {
            client.BaseAddress = new Uri("https://cbu.uz/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
