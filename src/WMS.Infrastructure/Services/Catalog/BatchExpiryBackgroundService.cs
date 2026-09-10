using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Catalog;

/// Runs the batch-expiry check for every active tenant once a day, so expiry
/// notifications appear without anyone calling the manual endpoint.
/// <remarks>
/// F6 (D12): har tenant O'Z scope'i va tenant konteksti bilan (<see cref="TenantScopes"/>) —
/// RLS ostida kontekstsiz so'rov 0 qator beradi, SQLite davridagidek bitta kontekstda
/// <c>tenantId</c> uzatib bo'lmaydi.
/// </remarks>
public class BatchExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _services;
    private readonly ILogger<BatchExpiryBackgroundService> _logger;

    public BatchExpiryBackgroundService(IServiceProvider services,
        ILogger<BatchExpiryBackgroundService> logger)
    {
        _services = services;
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
        try
        {
            // Bitta tenantning nosozligini TenantScopes o'zi logga yozadi va keyingisiga o'tadi.
            await TenantScopes.ForEachTenantAsync(_services, async (scope, tenantId, _) =>
            {
                var expiry = scope.GetRequiredService<IBatchExpiryService>();
                var created = await expiry.CheckBatchesAsync();
                if (created > 0)
                    _logger.LogInformation(
                        "Batch expiry check: {Count} notification(s) for tenant {TenantId}",
                        created, tenantId);
            }, _logger, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Tenant ro'yxatini o'qib bo'lmadi (baza vaqtincha yo'q) — fon xizmati o'lmasin,
            // ertaga yana urinadi.
            _logger.LogError(ex, "Batch expiry check: failed to load tenants");
        }
    }
}
