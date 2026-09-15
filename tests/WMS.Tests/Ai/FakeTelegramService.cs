using WMS.Application.Interfaces;
using WMS.Application.Telegram;

namespace WMS.Tests.Ai;

/// <summary>
/// Testdagi <see cref="ITelegramService"/> — Telegram API'siga CHIQMAYDI.
/// </summary>
/// <remarks>
/// Faqat yuborilgan xabarlarni yozib boradi: bot oqimida tekshiriladigan narsa —
/// «nima yuborildi va nechta bo'lakda», API'ning o'zi emas.
/// </remarks>
public sealed class FakeTelegramService : ITelegramService
{
    /// <summary>Yuborilgan matnlar (tartibda).</summary>
    public List<string> Sent { get; } = [];

    /// <summary>«Yozmoqda…» necha marta yuborildi.</summary>
    public int TypingCount { get; private set; }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public string? BotUsername => "test_bot";

    /// <summary>Testlar orasida holatni tozalaydi.</summary>
    public void Reset()
    {
        Sent.Clear();
        TypingCount = 0;
    }

    /// <inheritdoc />
    public Task<TelegramSendResult> SendMessageAsync(
        long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
    {
        Sent.Add(text);
        return Task.FromResult(TelegramSendResult.Success(Sent.Count));
    }

    /// <inheritdoc />
    public Task<bool> SendTypingAsync(long chatId, CancellationToken cancellationToken)
    {
        TypingCount++;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<TelegramBotInfo> GetMeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new TelegramBotInfo(true, "test_bot", null));

    /// <inheritdoc />
    public Task<TelegramSendResult> SendDocumentAsync(
        long chatId, string fileName, byte[] content, string? caption, CancellationToken cancellationToken) =>
        Task.FromResult(TelegramSendResult.Success(null));

    /// <inheritdoc />
    public Task<bool> AnswerCallbackQueryAsync(
        string callbackQueryId, string? text, bool showAlert, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    /// <inheritdoc />
    public Task<bool> EditMessageTextAsync(long chatId, long messageId, string text, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    /// <inheritdoc />
    public Task<bool> RemoveReplyMarkupAsync(long chatId, long messageId, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    /// <inheritdoc />
    public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long? offset, int timeoutSeconds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TelegramUpdate>>([]);

    /// <inheritdoc />
    public Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    /// <inheritdoc />
    public Task<bool> SetMyCommandsAsync(
        IReadOnlyList<TelegramBotCommand> commands, string? languageCode, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}
