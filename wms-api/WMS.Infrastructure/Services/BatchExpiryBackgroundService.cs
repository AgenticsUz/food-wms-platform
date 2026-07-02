using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// Runs the batch-expiry check for every active tenant once a day, so expiry
/// notifications appear without anyone calling the manual endpoint.
public class BatchExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BatchExpiryBackgroundService> _logger;

    public BatchExpiryBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<BatchExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small startup delay so migrations/seeding finish first.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCheckAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        var expiry = scope.ServiceProvider.GetRequiredService<IBatchExpiryService>();

        List<int> tenantIds;
        try
        {
            tenantIds = await db.Tenants.Where(t => t.IsActive)
                .Select(t => t.Id).ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch expiry check: failed to load tenants");
            return;
        }

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var created = await expiry.CheckBatchesAsync(tenantId);
                if (created > 0)
                    _logger.LogInformation(
                        "Batch expiry check: {Count} notification(s) for tenant {TenantId}",
                        created, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch expiry check failed for tenant {TenantId}", tenantId);
            }
        }
    }
}
