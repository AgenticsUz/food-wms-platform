namespace WMS.Domain.Enums;

/// <summary>
/// Yozuv qaysi yuzadan kirgan: ilova ekrani, Telegram boti yoki AI yordamchisi.
/// </summary>
/// <remarks>
/// Nega kerak: AI qatlami (F10·A3) hujjat va to'lov QORALAMASINI tayyorlaydi, yozuvni
/// esa odam tugma bosib yaratadi — keyin «buni kim/nima kiritdi?» degan savolga javob
/// beradigan yagona joy shu ustun bo'ladi. Auditda ham, hisobotda ham AI kiritgan
/// yozuvlarni ajratib ko'rish kerak bo'ladi (ishonchni o'lchash — A1/A3 qabul mezoni).
/// </remarks>
public enum DocumentSource
{
    /// <summary>Ilova ekrani (sukut).</summary>
    Ui = 1,

    /// <summary>Telegram boti (tugma yoki buyruq).</summary>
    Telegram = 2,

    /// <summary>AI yordamchisi tayyorlagan qoralama — tasdiqni odam bosgan.</summary>
    Ai = 3,
}
