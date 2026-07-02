using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class BatchExpiryService : IBatchExpiryService
{
    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;

    public BatchExpiryService(WmsDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<int> CheckBatchesAsync(int tenantId, int warningDaysAhead = 3)
    {
        var today = DateTime.UtcNow.Date;
        var warningCutoff = today.AddDays(warningDaysAhead);

        var batches = await _db.Batches
            .Include(b => b.Product)
            .Where(b => b.TenantId == tenantId
                && b.RemainingQuantity > 0
                && b.ExpiryDate != null)
            .ToListAsync();

        // Dedupe against ALL previous batch notifications, not just today's —
        // otherwise every expired batch re-notifies daily until it is emptied.
        var existingNotifications = await _db.Notifications
            .Where(n => n.TenantId == tenantId && n.EntityType == "Batch")
            .Select(n => new { n.EntityId, n.Type })
            .ToListAsync();

        var existingSet = existingNotifications
            .Where(n => n.EntityId.HasValue)
            .Select(n => (n.EntityId!.Value, n.Type))
            .ToHashSet();

        var created = 0;

        foreach (var batch in batches)
        {
            var expiry = batch.ExpiryDate!.Value.Date;

            if (expiry < today)
            {
                if (existingSet.Contains((batch.Id, NotificationType.BatchExpired)))
                    continue;
                await _notifications.CreateAsync(tenantId, null,
                    "Batch Expired",
                    $"{batch.Product.Name} partiyasi #{batch.LotNumber} muddati o'tdi!",
                    NotificationType.BatchExpired, "Batch", batch.Id);
                created++;
            }
            else if (expiry <= warningCutoff)
            {
                if (existingSet.Contains((batch.Id, NotificationType.BatchExpiring)))
                    continue;
                await _notifications.CreateAsync(tenantId, null,
                    "Batch Expiring",
                    $"{batch.Product.Name} partiyasi #{batch.LotNumber} muddati {expiry:yyyy-MM-dd} da tugaydi",
                    NotificationType.BatchExpiring, "Batch", batch.Id);
                created++;
            }
        }

        return created;
    }
}
