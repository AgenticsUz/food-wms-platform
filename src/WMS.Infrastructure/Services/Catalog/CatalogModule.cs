using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Catalog;

/// <summary>
/// W1·1 moduli — katalog (mahsulot, kategoriya, birlik) va ombor/zaxira (ombor, joy, partiya,
/// qoldiq, FEFO). Integrator <c>WmsModules</c> dan chaqiradi.
/// </summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IStockAllocator, StockAllocator>();
        services.AddScoped<IBatchExpiryService, BatchExpiryService>();

        // Sutkalik partiya muddati tekshiruvi API jarayoni ichida (D12): ikki kichik fon vazifasi
        // uchun alohida worker konteyneri ortiqcha.
        services.AddHostedService<BatchExpiryBackgroundService>();

        return services;
    }
}
