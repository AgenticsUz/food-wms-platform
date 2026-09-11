using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Catalog;

public class BatchExpiryService : IBatchExpiryService
{
    private const string BatchEntityType = "Batch";

    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;

    public BatchExpiryService(WmsDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<int> CheckBatchesAsync(int warningDaysAhead = 3)
    {
        var today = DateTime.UtcNow.Date;
        var warningCutoff = today.AddDays(warningDaysAhead);

        // Faqat ogohlantirish oynasiga tushganlar SQL'da tanlanadi (SQLite davrida qoldig'i bor
        // HAMMA partiya xotiraga tortilardi). `< cutoff + 1 kun` — pastdagi `.Date <= cutoff`
        // sharti bilan bir xil chegara.
        var cutoffExclusive = warningCutoff.AddDays(1);
        var batches = await _db.Batches
            .Where(b => b.RemainingQuantity > 0
                && b.ExpiryDate != null
                && b.ExpiryDate < cutoffExclusive)
            .Select(b => new { b.Id, b.LotNumber, b.ExpiryDate, ProductName = b.Product.Name })
            .ToListAsync();

        if (batches.Count == 0) return 0;

        // Dedupe against ALL previous batch notifications, not just today's —
        // otherwise every expired batch re-notifies daily until it is emptied.
        var batchIds = batches.Select(b => (Guid?)b.Id).ToList();
        var existingNotifications = await _db.Notifications
            .Where(n => n.EntityType == BatchEntityType && batchIds.Contains(n.EntityId))
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
                await _notifications.NotifyAsync(null,
                    NotificationMessages.BatchExpiredTitle, NotificationMessages.BatchExpired,
                    [batch.LotNumber, batch.ProductName],
                    NotificationType.BatchExpired, BatchEntityType, batch.Id);
                created++;
            }
            else if (expiry <= warningCutoff)
            {
                if (existingSet.Contains((batch.Id, NotificationType.BatchExpiring)))
                    continue;
                await _notifications.NotifyAsync(null,
                    NotificationMessages.BatchExpiringTitle, NotificationMessages.BatchExpiring,
                    [batch.LotNumber, batch.ProductName, expiry.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)],
                    NotificationType.BatchExpiring, BatchEntityType, batch.Id);
                created++;
            }
        }

        return created;
    }
}
