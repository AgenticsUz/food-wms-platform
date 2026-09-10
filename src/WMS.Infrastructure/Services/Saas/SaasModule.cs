using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Saas;

/// <summary>
/// W1·5 — SaaS qatlami: plan, feature, obuna, to'lov, suspend, brendlash, foydalanuvchi/rollar va
/// Console'ning WMS control plane'i (<c>/admin/v1</c>) servislari.
/// </summary>
public static class SaasModule
{
    public static IServiceCollection AddSaasModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IBrandingService, BrandingService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUserService, UserService>();

        // Holatsiz (faqat wwwroot yo'li) — singleton; PDF/Excel servislari (operations, reports) ham shuni oladi.
        services.AddSingleton<IBrandingFileStore, BrandingFileStore>();

        // D12: fon xizmati wms-api ichida (worker konteyneri yo'q).
        services.AddHostedService<SubscriptionExpiryBackgroundService>();

        return services;
    }
}
