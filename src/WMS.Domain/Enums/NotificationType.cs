namespace WMS.Domain.Enums;

public enum NotificationType
{
    Info = 1,
    Warning = 2,
    LowStock = 3,
    TransferConfirmed = 4,
    TransferRejected = 5,
    Error = 6,
    BatchExpiring = 7,
    BatchExpired = 8,
    ProductionStarted = 9,
    ProductionCompleted = 10,

    /// <summary>Qaytarish qabul qilindi (ilgari <see cref="Info"/> bilan yozilardi — TG3 filtri uchun ajratildi).</summary>
    ReturnReceived = 11,

    /// <summary>Sinov yoki to'lov muddati tugayapti (TG5, R6).</summary>
    SubscriptionWarning = 12,

    /// <summary>Hisob to'xtatildi (TG5).</summary>
    SubscriptionSuspended = 13,

    /// <summary>Tarif limitiga yaqinlashildi (TG5, R7).</summary>
    LimitWarning = 14,

    /// <summary>Yangi transfer tasdiq kutmoqda — Telegram'da «Tasdiqlash/Rad etish» tugmalari (TG9).</summary>
    TransferPending = 15,

    /// <summary>Yangi ishlab chiqarish buyurtmasi — «Boshlash» tugmasi (TG9).</summary>
    ProductionPending = 16,
}
