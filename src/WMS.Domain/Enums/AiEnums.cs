namespace WMS.Domain.Enums;

/// <summary>Suhbat qaysi yuzada bormoqda.</summary>
/// <remarks>
/// Bitta gateway ikkala kanalga xizmat qiladi, lekin javob shakli boshqa: web strukturali
/// natijani komponent bilan chizadi, Telegram esa faqat matn ko'radi. Kanal suhbat boshida
/// bir marta yoziladi va ro'yxatlarda filtr bo'lib ham ishlaydi.
/// </remarks>
public enum AiChannel
{
    /// <summary>wms-web yon paneli.</summary>
    Web = 1,

    /// <summary>Telegram boti.</summary>
    Telegram = 2,
}

/// <summary>Suhbatdagi xabar egasi.</summary>
public enum AiMessageRole
{
    /// <summary>Foydalanuvchi savoli.</summary>
    User = 1,

    /// <summary>Model javobi (matn va/yoki tool chaqiriqlari).</summary>
    Assistant = 2,

    /// <summary>
    /// Bajarilgan tool natijalari.
    /// </summary>
    /// <remarks>
    /// Provayder shaklida bu «user» xabari, lekin tarixda ALOHIDA rol sifatida saqlanadi:
    /// aks holda ro'yxatda foydalanuvchi yozgan savol bilan tizim qaytargan JSON bir xil
    /// ko'rinardi va «AI buni qayerdan oldi?» degan savolga javob berib bo'lmasdi.
    /// </remarks>
    Tool = 3,
}
