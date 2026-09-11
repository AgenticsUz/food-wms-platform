namespace WMS.Domain.Enums;

/// <summary>Telegram navbatidagi xabarning holati (Wash <c>MessageStatus</c> bilan bir xil ma'no).</summary>
public enum TelegramOutboxStatus
{
    Pending = 1,
    Sent = 2,

    /// <summary>Urinishlar tugadi yoki transport ishlamadi — XATO, egaga ko'rinadi.</summary>
    Failed = 3,

    /// <summary>
    /// Yuborilmadi, lekin xato EMAS: chat bloklangan yoki ulanish uzilgan. Aks holda jurnal
    /// kundalik «xatolar» bilan to'lib, haqiqiy nosozlik ko'rinmay qolardi.
    /// </summary>
    Skipped = 4,
}

/// <summary>Navbat qatori nima qiladi.</summary>
public enum TelegramOutboxKind
{
    Message = 1,

    /// <summary>
    /// Ilgari yuborilgan tugmali xabardan tugmalarni olib tashlash — amal web'dan yoki boshqa
    /// menejer tomonidan bajarilganda (TG9). So'rov ichida emas, navbat orqali.
    /// </summary>
    RemoveButtons = 2,
}
