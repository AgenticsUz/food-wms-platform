using WMS.Infrastructure.Services;
using WMS.Infrastructure.Services.Catalog;
using WMS.Infrastructure.Services.Operations;
using WMS.Infrastructure.Services.Reports;
using WMS.Infrastructure.Services.Saas;
using WMS.Infrastructure.Services.Trade;

namespace WMS.API.Hosting;

/// <summary>
/// Modul servislarining YAGONA ulanish nuqtasi — integratorniki (HOLAT §5: yagona ulash nuqtasi
/// hech bir modul agentiga berilmaydi, aks holda parallel agentlar shu faylda to'qnashadi).
/// </summary>
/// <remarks>
/// Har modul o'z <c>Add&lt;Modul&gt;Module()</c> kengaytmasini o'z papkasida yozadi
/// (<c>WMS.Infrastructure/Services/&lt;Modul&gt;/</c>), bu yerga faqat chaqiruv qo'shiladi.
/// </remarks>
public static class WmsModules
{
    public static IServiceCollection AddWmsModules(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCatalogModule();
        services.AddTradeModule();
        services.AddProductionModule();
        services.AddOperationsModule();
        services.AddReportsModule();
        services.AddSaasModule();

        return services;
    }
}
