using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Trade;

/// <summary>
/// W1·2 moduli — transfer, kontragent, agent, moliya (transfer PDF'i va kabinet yuzasi).
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

        // Narx taklifi (P2.2): hujjat qatorlari ustidan o'qiydi — shuning uchun transfer
        // moduli bilan bir joyda, alohida modul emas.
        services.AddScoped<IPricingService, PricingService>();

        // Kabinet (F9): kontragent/agent o'z oldi-berdisini ko'radi; hisobni admin biriktiradi.
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IPortalAccountService, PortalAccountService>();

        return services;
    }
}
