using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Trade;

/// <summary>
/// W1·2 moduli — transfer, kontragent, agent, moliya (va transfer PDF'i).
/// </summary>
/// <remarks>
/// Hammasi scoped: servislar so'rovning <c>WmsDbContext</c> iga (va u orqali tenant kontekstiga)
/// bog'langan. Fon xizmati yo'q. Ulash — integratorda (<c>WmsModules</c>).
/// </remarks>
public static class TradeModule
{
    public static IServiceCollection AddTradeModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ITransferPdfService, TransferPdfService>();
        services.AddScoped<ICounterpartyService, CounterpartyService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IFinanceService, FinanceService>();

        return services;
    }
}
