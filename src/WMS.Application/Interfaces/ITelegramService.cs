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
public sealed record TelegramSendResult(bool Ok, int StatusCode, int? RetryAfterSeconds, string? Description)
{
    public static TelegramSendResult Success() => new(true, 200, null, null);

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
    Task<TelegramSendResult> SendMessageAsync(long chatId, string text, CancellationToken cancellationToken);

    /// <summary>Long-poll (<c>getUpdates</c>); xatoda istisno tashlaydi — polling xizmati kutib qayta uradi.</summary>
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>Polling bilan webhook bir vaqtda ishlamaydi — startupda chaqiriladi.</summary>
    Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken);

    Task<bool> SetMyCommandsAsync(IReadOnlyList<TelegramBotCommand> commands, string? languageCode, CancellationToken cancellationToken);
}
