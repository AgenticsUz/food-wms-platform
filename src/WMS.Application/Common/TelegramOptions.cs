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

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);
}
