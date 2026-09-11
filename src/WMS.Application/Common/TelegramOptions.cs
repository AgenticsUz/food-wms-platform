namespace WMS.Application.Common;

/// <summary>
/// <c>Telegram</c> bo'limi (env: <c>Telegram__BotToken</c> …). Token bo'sh — bot BUTUNLAY o'chiq:
/// polling ham, navbat ham ishga tushmaydi, profil sahifasi «sozlanmagan» deydi.
/// </summary>
/// <remarks>
/// Bot username'i BU YERDA EMAS — startupda <c>getMe</c> dan olinadi (Wash'da tenant botlari
/// katalogi bor, bizda bitta umumiy bot; ikkinchi manba ajralib ketardi).
/// </remarks>
public class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary><c>@BotFather</c> tokeni. Dev va prod — BOSHQA botlar: polling bir tokenda bitta iste'molchi.</summary>
    public string? BotToken { get; set; }

    /// <summary>Polling xatosidan keyin va navbat yugurishlari orasidagi kutish.</summary>
    public int PollingIntervalSeconds { get; set; } = 2;

    /// <summary><c>getUpdates</c> long-poll muddati (Telegram sukuti 0 — darhol qaytadi).</summary>
    public int LongPollTimeoutSeconds { get; set; } = 30;

    /// <summary>Deep-link tokenining umri.</summary>
    public int LinkTokenMinutes { get; set; } = 10;

    /// <summary>Bir yugurishda navbatdan nechta xabar olinadi.</summary>
    public int OutboxBatchSize { get; set; } = 50;

    /// <summary>Yuborilgan/xato qatorlar shuncha kundan keyin o'chiriladi.</summary>
    public int OutboxRetentionDays { get; set; } = 14;

    /// <summary>Telegram tili <c>uz</c>/<c>ru</c> bo'lmasa bot shu tilda gapiradi.</summary>
    public string DefaultLanguage { get; set; } = "uz";

    /// <summary>
    /// Platforma egasining chati (TG17): yangi tenant, obuna hodisalari, navbat/polling nosozliklari,
    /// API start. Bo'sh — o'chiq. Tenant ma'lumoti (mahsulot, summa) bu kanalga BORMAYDI.
    /// </summary>
    public long? OpsChatId { get; set; }

    /// <summary>Kunlik xulosa soati, Toshkent vaqti (TG11).</summary>
    public int DigestHour { get; set; } = 8;

    /// <summary>
    /// Tinch soatlar, Toshkent vaqti (TG11): oddiy (shoshilinch bo'lmagan) xabarlar
    /// <see cref="QuietFromHour"/> dan <see cref="QuietToHour"/> gacha ushlab turiladi. Teng bo'lsa — o'chiq.
    /// </summary>
    public int QuietFromHour { get; set; } = 22;
    public int QuietToHour { get; set; } = 7;

    /// <summary>
    /// wms-web manzili (<c>https://wms.agentics.uz</c>) — xabardagi «Ochish» havolasi uchun.
    /// So'rov <c>Host</c> idan olinmaydi: fon xizmatida so'rov yo'q, nginx orqasida esa u ichki nom.
    /// Bo'sh — havola qo'yilmaydi.
    /// </summary>
    public string? WebUrl { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);
}
