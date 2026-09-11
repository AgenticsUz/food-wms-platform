using System.Globalization;

namespace WMS.Application.Common.Localization;

/// <summary>
/// Bildirishnoma sarlavha va matn KALITLARI (inglizcha shablon = tarjima kaliti, <see cref="Messages"/> kabi).
/// Argumentlar tayyor satr: raqam/sana chaqiruvchida formatlanadi (<see cref="Amount"/>, <see cref="Date"/>).
/// </summary>
public static class NotificationMessages
{
    // ── Sarlavhalar ──
    public const string TransferConfirmedTitle = "Transfer Confirmed";
    public const string TransferRejectedTitle = "Transfer Rejected";
    public const string ReturnReceivedTitle = "Return Received";
    public const string LowStockTitle = "Low Stock Alert";
    public const string BatchExpiredTitle = "Batch Expired";
    public const string BatchExpiringTitle = "Batch Expiring";
    public const string ProductionStartedTitle = "Production Started";
    public const string ProductionCompletedTitle = "Production Completed";

    // ── Matnlar ──
    /// <summary>{0} kontragent, {1} sana, {2} summa.</summary>
    public const string IncomingConfirmed = "Incoming from {0} ({1}) confirmed. Amount: {2}";
    public const string SaleConfirmed = "Sale to {0} ({1}) confirmed. Amount: {2}";
    /// <summary>{0} qaysi ombordan, {1} qaysi omborga, {2} sana.</summary>
    public const string InternalConfirmed = "Internal transfer {0} → {1} ({2}) confirmed";
    public const string ReturnReceived = "Return from {0} ({1}) received. Amount: {2}";
    /// <summary>{0} kontragent yoki ombor, {1} sana.</summary>
    public const string TransferRejected = "Transfer {0} ({1}) rejected";
    /// <summary>{0} mahsulot, {1} qoldiq, {2} birlik, {3} minimum.</summary>
    public const string LowStock = "{0}: {1} {2} left, minimum {3}";
    /// <summary>{0} partiya, {1} mahsulot, {2} sana.</summary>
    public const string BatchExpired = "Batch {0} of {1} has expired";
    public const string BatchExpiring = "Batch {0} of {1} expires on {2}";
    /// <summary>{0} mahsulot, {1} sana, {2} javobgar.</summary>
    public const string ProductionStarted = "Order {0} ({1}) started. Responsible: {2}";
    /// <summary>{0} mahsulot, {1} sana, {2} miqdor, {3} birlik.</summary>
    public const string ProductionCompleted = "Order {0} ({1}) completed. Output: {2} {3}";

    // ── Tugmali (TG9): kutilayotgan tasdiq / yangi buyurtma ──
    public const string TransferPendingTitle = "Transfer Awaiting Confirmation";
    public const string ProductionPendingTitle = "Production Order Created";
    /// <summary>{0} kontragent, {1} sana, {2} summa, {3} kim yaratdi.</summary>
    public const string IncomingPending = "Incoming from {0} ({1}), amount {2}, created by {3}";
    public const string SalePending = "Sale to {0} ({1}), amount {2}, created by {3}";
    /// <summary>{0} qaysi ombordan, {1} qaysi omborga, {2} sana, {3} kim yaratdi.</summary>
    public const string InternalPending = "Internal transfer {0} → {1} ({2}), created by {3}";
    public const string ReturnPending = "Return from {0} ({1}), amount {2}, created by {3}";
    /// <summary>{0} mahsulot, {1} sana, {2} miqdor, {3} birlik.</summary>
    public const string ProductionPending = "Order {0} ({1}) is planned. Quantity: {2} {3}";

    // ── Yetkazish (TG12) ──
    public const string DeliveryStopFailedTitle = "Delivery Stop Failed";
    /// <summary>{0} haydovchi, {1} kontragent, {2} sana.</summary>
    public const string DeliveryStopFailed = "{0} could not deliver to {1} ({2})";

    // ── Mijoz xabarlari (TG13): {0} — zavod nomi ──
    public const string ClientOrderConfirmed = "Your order from {0} ({1}) is confirmed. Amount: {2}";
    public const string ClientOnTheWay = "Your order from {0} is on the way today ({1}).";
    public const string ClientDelivered = "Your order from {0} has been delivered. Thank you!";
    /// <summary>{0} zavod, {1} summa, {2} qoldiq balans.</summary>
    public const string ClientPaymentReceived = "{0}: payment of {1} received. Your balance: {2}";
    public const string ClientDebtReminder = "Reminder from {0}: your outstanding balance is {1}. Please settle it.";
    public const string ClientBalance = "Your balance with {0}: {1}";
    public const string ClientNoDebt = "You have no outstanding balance with {0}.";

    // ── Obuna va tarif (TG5) ──
    public const string SubscriptionExpiringTitle = "Subscription Expiring";
    public const string SubscriptionSuspendedTitle = "Account Suspended";
    public const string PlanLimitTitle = "Plan Limit";
    /// <summary>{0} sana.</summary>
    public const string TrialEnding = "Your trial ends on {0}. Choose a plan to continue.";
    public const string PaidEnding = "Your paid period ends on {0}. Contact us to renew.";
    public const string SuspendedNonPayment = "Your account is suspended for non-payment. Contact us to restore access.";

    /// <summary>Telegram xabaridagi havola matni.</summary>
    public const string Open = "Open";

    /// <summary>Pul: <c>1 250 000</c> (bo'sh joy ming ajratgichi — uz/ru uchun tabiiy).</summary>
    public static string Amount(decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');

    /// <summary>Miqdor: butun bo'lsa <c>120</c>, aks holda <c>12.5</c>.</summary>
    public static string Quantity(decimal value) =>
        (value == Math.Truncate(value) ? value.ToString("N0", CultureInfo.InvariantCulture) : value.ToString("0.###", CultureInfo.InvariantCulture))
        .Replace(',', ' ');

    /// <summary>
    /// Sana Toshkent vaqtida (UTC+5). ⚠️ Vaqt mintaqasi — HOLAT'dagi «keyinga qolgan qaror»;
    /// qaror chiqsa BITTA shu joy o'zgaradi (AnalyticsService'da ham +5 konstanta).
    /// </summary>
    public static string Date(DateTime utc) =>
        utc.AddHours(5).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}
