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
