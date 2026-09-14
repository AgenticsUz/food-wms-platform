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

        // Nom qidiruvi (P2.1) katalog modulida turadi, lekin kontragent/ombor uchun ham
        // ishlaydi: uchala nom bitta normalizator va bitta `pg_trgm` qoidasiga bo'ysunadi,
        // ya'ni uni bo'lib yuborish uch xil qidiruv xatti-harakatini keltirib chiqarardi.
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IBatchExpiryService, BatchExpiryService>();

        // Sutkalik partiya muddati tekshiruvi API jarayoni ichida (D12): ikki kichik fon vazifasi
        // uchun alohida worker konteyneri ortiqcha.
        services.AddHostedService<BatchExpiryBackgroundService>();

        return services;
    }
}
