using System.Globalization;
using WMS.Domain.Enums;

namespace WMS.Application.Common;

/// <summary>
/// Bildirishnoma turi → kim oladi (ruxsat), qanday ko'rinadi (emoji), qayerga olib boradi (havola).
/// Telegram kanali uchun (TG3/TG4); ilova ichidagi bildirishnomani hamma ko'raveradi.
/// </summary>
public static class NotificationRouting
{
    /// <summary>Kim uchun bildirishnoma foydali. <see langword="null"/> — hammaga.</summary>
    public static string? RequiredPermission(NotificationType type) => type switch
    {
        NotificationType.LowStock or NotificationType.BatchExpiring or NotificationType.BatchExpired
            => WmsPermissions.WarehouseView,
        NotificationType.TransferConfirmed or NotificationType.TransferRejected or NotificationType.ReturnReceived
            => WmsPermissions.TransfersView,
        NotificationType.ProductionStarted or NotificationType.ProductionCompleted
            => WmsPermissions.ProductionView,
        // Tugmali xabarlar — faqat amalni bajara oladiganlarga (TG9).
        NotificationType.TransferPending => WmsPermissions.TransfersConfirm,
        NotificationType.ProductionPending => WmsPermissions.ProductionManage,
        // Obuna va limit — tenant egasining ishi (amalda admin).
        NotificationType.SubscriptionWarning or NotificationType.SubscriptionSuspended or NotificationType.LimitWarning
            => WmsPermissions.SettingsModules,
        _ => null,
    };

    /// <summary>
    /// Shoshilinch — tinch soatlarda ham darhol (TG11): tasdiq kutayotgan transfer, kam zaxira, muddati
    /// o'tgan partiya, obuna. Qolgani (bosqich tugadi, tasdiqlandi …) ertalabgacha kutadi.
    /// </summary>
    public static bool IsUrgent(NotificationType type) => type switch
    {
        NotificationType.TransferPending or NotificationType.LowStock or NotificationType.BatchExpired
            or NotificationType.SubscriptionWarning or NotificationType.SubscriptionSuspended
            or NotificationType.Error or NotificationType.Warning => true,
        _ => false,
    };

    public static string Emoji(NotificationType type) => type switch
    {
        NotificationType.LowStock or NotificationType.BatchExpiring => "⚠️",
        NotificationType.BatchExpired => "⛔",
        NotificationType.TransferConfirmed or NotificationType.ProductionCompleted => "✅",
        NotificationType.TransferRejected => "❌",
        NotificationType.ProductionStarted => "▶️",
        NotificationType.TransferPending or NotificationType.ProductionPending => "🕓",
        NotificationType.ReturnReceived => "↩️",
        NotificationType.SubscriptionWarning or NotificationType.LimitWarning => "💳",
        NotificationType.SubscriptionSuspended => "🚫",
        NotificationType.Error => "🔴",
        _ => "ℹ️",
    };

    /// <summary>
    /// wms-web'dagi sahifa (bosh <c>/</c> siz). Batafsil sahifasi yo'q entity'lar ro'yxatga olib
    /// boradi — buzuq havola yo'q havoladan yomon. <see langword="null"/> — havola qo'yilmaydi.
    /// </summary>
    public static string? LinkPath(string? entityType, Guid? entityId)
    {
        string? id = entityId?.ToString("D", CultureInfo.InvariantCulture);
        return entityType switch
        {
            "Transfer" when id is not null => $"transfers/{id}",
            "ProductionOrder" when id is not null => $"production/orders/{id}",
            "Product" => "products",
            "Batch" => "warehouse/batches",
            "Tenant" or "Plan" => "settings/subscription",
            _ => null,
        };
    }

    /// <summary>
    /// Tugmali bildirishnoma qaysi amalni yopadi: tasdiq/rad kelganda TransferPending tugmalari,
    /// boshlash kelganda ProductionPending tugmalari olib tashlanadi (TG9).
    /// </summary>
    public static NotificationType? ClosesPending(NotificationType type) => type switch
    {
        NotificationType.TransferConfirmed or NotificationType.TransferRejected or NotificationType.ReturnReceived
            => NotificationType.TransferPending,
        NotificationType.ProductionStarted => NotificationType.ProductionPending,
        _ => null,
    };
}
