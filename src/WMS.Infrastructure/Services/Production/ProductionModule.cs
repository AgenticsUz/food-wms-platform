using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

/// <summary>
/// W1·3 moduli — ishlab chiqarish (bosqich, retsept, buyurtma) va sifat nazorati.
/// </summary>
/// <remarks>
/// Ulash nuqtasi integratorniki (<c>WmsModules.AddWmsModules</c>) — parallel agentlar bitta
/// faylda to'qnashmasin. Servislar scoped: har biri so'rovning <c>WmsDbContext</c>'i (va shu
/// orqali tenant konteksti) bilan ishlaydi. Fon xizmati yo'q.
/// </remarks>
public static class ProductionModule
{
    public static IServiceCollection AddProductionModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<IQcService, QcService>();
        return services;
    }
}
