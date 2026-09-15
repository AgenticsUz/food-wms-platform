using WMS.Application.Telegram;

namespace WMS.Application.Interfaces;

/// <summary>Bot haqidagi ma'lumot (<c>getMe</c>).</summary>
public sealed record TelegramBotInfo(bool Ok, string? Username, string? Error);

/// <summary>Bot menyusidagi buyruq (<c>setMyCommands</c>).</summary>
public sealed record TelegramBotCommand(string Command, string Description);

/// <summary><c>sendMessage</c> natijasi — navbat shu bo'yicha qayta urinadi yoki ulanishni uzadi.</summary>
/// <param name="StatusCode">HTTP kodi; tarmoq xatosida 0.</param>
/// <param name="RetryAfterSeconds">429 dagi <c>parameters.retry_after</c>.</param>
/// <param name="Description">Telegram <c>description</c> yoki istisno TURI (matni emas — unda manzil bo'lishi mumkin).</param>
/// <param name="MessageId">Yuborilgan xabarning id'si — tugmali xabarni keyin tahrirlash uchun (TG9).</param>
public sealed record TelegramSendResult(bool Ok, int StatusCode, int? RetryAfterSeconds, string? Description, long? MessageId = null)
{
    public static TelegramSendResult Success(long? messageId) => new(true, 200, null, null, messageId);

    /// <summary>Foydalanuvchi botni bloklagan yoki chat yo'q — qayta urinish befoyda, ulanish uziladi.</summary>
    public bool IsChatGone =>
        StatusCode == 403
        || (StatusCode == 400 && Description is { } d && d.Contains("chat not found", StringComparison.OrdinalIgnoreCase));

    public bool IsRateLimited => StatusCode == 429;
}

/// <summary>
/// Telegram Bot API. Token sozlanmagan bo'lsa (sukut) — <see cref="IsEnabled"/> yolg'on va hamma
/// metod no-op: hech qanday tashqi chaqiruv bo'lmaydi.
/// </summary>
/// <remarks>
/// Singleton. Bot username'i konfiguratsiyada emas — <see cref="GetMeAsync"/> bir marta olib keshlaydi.
/// </remarks>
public interface ITelegramService
{
    bool IsEnabled { get; }

    /// <summary><c>@</c> siz; <see cref="GetMeAsync"/> muvaffaqiyatli o'tmaguncha <see langword="null"/>.</summary>
    string? BotUsername { get; }

    Task<TelegramBotInfo> GetMeAsync(CancellationToken cancellationToken);

    /// <summary>HTML rejimida yuboradi. Istisno tashlamaydi — natija qaytaradi.</summary>
    /// <param name="replyMarkupJson">Inline tugmalar (<c>reply_markup</c> JSON) yoki <see langword="null"/>.</param>
    Task<TelegramSendResult> SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken);

    /// <summary>Fayl (Excel/PDF) yuboradi — ≤ 50 MB (Telegram bot chegarasi). Istisno tashlamaydi.</summary>
    Task<TelegramSendResult> SendDocumentAsync(long chatId, string fileName, byte[] content, string? caption, CancellationToken cancellationToken);

    /// <summary>Tugma bosilganiga javob (Telegram 30 s kutadi); <paramref name="showAlert"/> — modal oyna.</summary>
    Task<bool> AnswerCallbackQueryAsync(string callbackQueryId, string? text, bool showAlert, CancellationToken cancellationToken);

    /// <summary>«Yozmoqda…» ko'rsatkichi (F10·A1: AI javobi bir necha soniya olishi mumkin).</summary>
    /// <param name="chatId">Chat.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Yuborildimi.</returns>
    /// <remarks>
    /// ⚠️ Belgi ~5 soniyada o'chadi va uzunroq javobda qayta yuborilishi kerak. Natija
    /// TEKSHIRILMAYDI: ko'rsatkich yuborilmasa ham javobning o'zi baribir ketadi.
    /// </remarks>
    Task<bool> SendTypingAsync(long chatId, CancellationToken cancellationToken);

    /// <summary>Xabar matnini almashtiradi va tugmalarni OLIB TASHLAYDI (natija yozuvi bilan).</summary>
    Task<bool> EditMessageTextAsync(long chatId, long messageId, string text, CancellationToken cancellationToken);

    /// <summary>Faqat tugmalarni olib tashlaydi (matn qoladi) — web'dan bajarilgan amal uchun.</summary>
    Task<bool> RemoveReplyMarkupAsync(long chatId, long messageId, CancellationToken cancellationToken);

    /// <summary>Long-poll (<c>getUpdates</c>); xatoda istisno tashlaydi — polling xizmati kutib qayta uradi.</summary>
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>Polling bilan webhook bir vaqtda ishlamaydi — startupda chaqiriladi.</summary>
    Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken);

    Task<bool> SetMyCommandsAsync(IReadOnlyList<TelegramBotCommand> commands, string? languageCode, CancellationToken cancellationToken);
}
