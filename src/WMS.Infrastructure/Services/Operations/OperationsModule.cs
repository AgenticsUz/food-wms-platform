using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Operations;

/// <summary>
/// W1·4 moduli: yetkazish, KPI/smena/davomat, bildirishnoma, tenant auditi, Telegram.
/// </summary>
public static class OperationsModule
{
    public static IServiceCollection AddOperationsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IDeliveryPdfService, DeliveryPdfService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();

        // ⚠️ RemoveAllLoggers: IHttpClientFactory har so'rovning URI'sini logga yozadi, Bot API
        // tokeni esa URI ichida — usiz bot tokeni har bildirishnomada stdout'ga (va log
        // yig'uvchisiga) tushardi. 10 soniya — SQLite davridagi qiymat: Telegram osilib qolsa
        // tasdiq/rad kabi so'rov shuncha kutadi, undan ortiq emas.
        services.AddHttpClient(TelegramService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .RemoveAllLoggers();

        // Singleton: token bir marta o'qiladi, «sozlanmagan» xabari bir marta logga tushadi.
        services.AddSingleton<ITelegramService, TelegramService>();

        return services;
    }
}
