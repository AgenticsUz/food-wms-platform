using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Services.Operations.Telegram;

namespace WMS.Infrastructure.Services.Operations;

/// <summary>
/// W1·4 moduli: yetkazish, KPI/smena/davomat, bildirishnoma, tenant auditi, Telegram (polling + navbat).
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
        // tokeni esa URI ichida — usiz bot tokeni har chaqiruvda stdout'ga (va log yig'uvchisiga)
        // tushardi. 10 soniya — sendMessage/getMe uchun yetarli; long-poll'ga alohida client:
        // getUpdates 30 s kutadi va shu timeout ichiga sig'masdi.
        services.AddHttpClient(TelegramService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .RemoveAllLoggers();

        services.AddHttpClient(TelegramService.PollingHttpClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org/");
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .RemoveAllLoggers();

        // Singleton: token bir marta o'qiladi, «sozlanmagan» xabari bir marta logga tushadi, bot username keshi.
        services.AddSingleton<ITelegramService, TelegramService>();
        services.AddScoped<ITelegramLinkService, TelegramLinkService>();
        services.AddScoped<ITelegramUpdateHandler, TelegramUpdateHandler>();
        services.AddScoped<TelegramCallbackExecutor>();
        services.AddScoped<TelegramQueryCommands>();
        services.AddScoped<TelegramChatContext>();
        services.AddScoped<TelegramWorkCommands>();
        services.AddScoped<TelegramGroupCommands>();
        services.AddScoped<TelegramPartnerBot>();
        services.AddScoped<ITelegramPartnerNotifier>(sp => sp.GetRequiredService<TelegramPartnerBot>());
        services.AddScoped<IOpsNotifier, TelegramOpsNotifier>();
        services.AddHostedService<TelegramDigestBackgroundService>();

        // Token bo'sh bo'lsa ikkalasi darhol chiqadi (D12: fon ishlari API jarayonida, worker yo'q).
        services.AddHostedService<TelegramPollingBackgroundService>();
        services.AddHostedService<TelegramOutboxBackgroundService>();

        return services;
    }
}
