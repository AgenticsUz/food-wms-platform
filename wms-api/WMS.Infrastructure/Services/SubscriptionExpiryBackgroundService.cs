using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>
/// Kuniga bir marta muddati (trial + grace) o'tgan trial tenantlarni Suspended holatiga
/// o'tkazadi. Ma'lumot O'CHIRILMAYDI — mijoz plan tanlasa hammasi joyida qoladi.
///
/// Bu fon ishi bo'lmasa ham kirish bloklanadi (SubscriptionPolicy har so'rovda ishlaydi);
/// bu yerda holat control plane statistikasida to'g'ri ko'rinishi uchun yangilanadi.
/// </summary>
public class SubscriptionExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpiryBackgroundService> _logger;
    private readonly SubscriptionOptions _options;

    public SubscriptionExpiryBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpiryBackgroundService> logger, IOptions<SubscriptionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            var state = scope.ServiceProvider.GetRequiredService<ITenantStateService>();

            var cutoff = DateTime.UtcNow.AddDays(-_options.GraceDays);
            var expired = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Trial
                            && t.TrialEndsAt != null && t.TrialEndsAt < cutoff)
                .ToListAsync(ct);

            if (expired.Count == 0) return;

            foreach (var tenant in expired)
                tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
            await db.SaveChangesAsync(ct);

            foreach (var tenant in expired)
                state.Invalidate(tenant.Id);

            _logger.LogInformation("Subscription expiry: {Count} trial tenant(s) suspended", expired.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription expiry check failed");
        }
    }
}
