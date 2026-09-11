using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Telegram Bot API'ning HTTP qatlami. <c>Telegram:BotToken</c> sozlanmagan bo'lsa butunlay no-op.
/// </summary>
/// <remarks>
/// <para>
/// Singleton: token konfiguratsiyadan bir marta o'qiladi, «sozlanmagan» xabari logga BIR MARTA tushadi.
/// Token sozlanmaganligi xato EMAS — Telegram ixtiyoriy kanal.
/// </para>
/// <para>
/// ⚠️ Token so'rov URI'sida (<c>/bot{token}/…</c>). Shuning uchun: nomlangan client'larning loggerlari
/// o'chirilgan (<c>OperationsModule</c>), istisno obyekti logga BERILMAYDI — matni token yashirilgan
/// holda. Telegram <c>description</c> matni xavfsiz: unda token yo'q.
/// </para>
/// </remarks>
public sealed class TelegramService : ITelegramService
{
    /// <summary>Qisqa chaqiruvlar (10 s): sendMessage, getMe, setMyCommands.</summary>
    public const string HttpClientName = "telegram";

    /// <summary>Long-poll (<c>getUpdates</c>) — 30 s kutadi, timeout undan uzun bo'lishi shart.</summary>
    public const string PollingHttpClientName = "telegram-poll";

    private const int MaxDescriptionLength = 300;
    private static readonly string[] AllowedUpdates = ["message", "my_chat_member", "callback_query"];
    private static readonly JsonSerializerOptions BodyJson = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<TelegramService> _logger;
    private readonly string? _token;

    public TelegramService(IHttpClientFactory httpFactory, IOptions<TelegramOptions> options, ILogger<TelegramService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _httpFactory = httpFactory;
        _logger = logger;
        _token = options.Value.BotToken?.Trim();

        if (!IsEnabled)
            _logger.LogInformation("Telegram:BotToken sozlanmagan — bot o'chiq, bildirishnomalar Telegram'ga yuborilmaydi");
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_token);

    public string? BotUsername { get; private set; }

    public async Task<TelegramBotInfo> GetMeAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled) return new TelegramBotInfo(false, null, "not configured");

        ApiCall call = await CallAsync(HttpClientName, "getMe", null, cancellationToken);
        if (!call.Ok) return new TelegramBotInfo(false, null, call.Description);

        string? username = call.Result.ValueKind == JsonValueKind.Object
                           && call.Result.TryGetProperty("username", out JsonElement name)
            ? name.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(username)) BotUsername = username;
        return new TelegramBotInfo(true, username, null);
    }

    public async Task<TelegramSendResult> SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
    {
        if (!IsEnabled || chatId == 0) return new TelegramSendResult(false, 0, null, "not configured");

        // reply_markup tayyor JSON (bazada shunday saqlanadi) — qayta serializatsiya qilinmasin.
        ApiCall call = await CallAsync(HttpClientName, "sendMessage",
            new SendMessageBody(chatId, text, "HTML", true, ParseMarkup(replyMarkupJson)),
            cancellationToken);

        if (!call.Ok)
            return new TelegramSendResult(false, call.StatusCode, call.RetryAfterSeconds, call.Description);

        long? messageId = call.Result.ValueKind == JsonValueKind.Object
                          && call.Result.TryGetProperty("message_id", out JsonElement id)
                          && id.TryGetInt64(out long parsed)
            ? parsed
            : null;
        return TelegramSendResult.Success(messageId);
    }

    public async Task<TelegramSendResult> SendDocumentAsync(long chatId, string fileName, byte[] content, string? caption, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!IsEnabled || chatId == 0) return new TelegramSendResult(false, 0, null, "not configured");

        try
        {
            HttpClient client = _httpFactory.CreateClient(PollingHttpClientName); // katta fayl — uzunroq timeout
            Uri uri = new($"{client.BaseAddress}bot{_token}/sendDocument");

            using MultipartFormDataContent form = new();
            form.Add(new StringContent(chatId.ToString(System.Globalization.CultureInfo.InvariantCulture)), "chat_id");
            if (!string.IsNullOrWhiteSpace(caption))
            {
                form.Add(new StringContent(caption), "caption");
                form.Add(new StringContent("HTML"), "parse_mode");
            }
            ByteArrayContent file = new(content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "document", fileName);

            using HttpResponseMessage response = await client.PostAsync(uri, form, cancellationToken);
            string raw = await response.Content.ReadAsStringAsync(cancellationToken);
            ApiCall call = Read(raw, (int)response.StatusCode);
            return call.Ok
                ? TelegramSendResult.Success(null)
                : new TelegramSendResult(false, call.StatusCode, call.RetryAfterSeconds, call.Description);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // Best-effort transport.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning("Telegram 'sendDocument' chaqiruvi yiqildi: {ErrorType}: {Message}", ex.GetType().Name, Redact(ex.GetBaseException().Message));
            return new TelegramSendResult(false, 0, null, ex.GetType().Name);
        }
    }

    public async Task<bool> AnswerCallbackQueryAsync(string callbackQueryId, string? text, bool showAlert, CancellationToken cancellationToken)
    {
        if (!IsEnabled) return false;
        ApiCall call = await CallAsync(HttpClientName, "answerCallbackQuery",
            new { callback_query_id = callbackQueryId, text, show_alert = showAlert }, cancellationToken);
        return call.Ok;
    }

    public async Task<bool> EditMessageTextAsync(long chatId, long messageId, string text, CancellationToken cancellationToken)
    {
        if (!IsEnabled) return false;
        ApiCall call = await CallAsync(HttpClientName, "editMessageText",
            new { chat_id = chatId, message_id = messageId, text, parse_mode = "HTML", disable_web_page_preview = true },
            cancellationToken);
        return call.Ok;
    }

    public async Task<bool> RemoveReplyMarkupAsync(long chatId, long messageId, CancellationToken cancellationToken)
    {
        if (!IsEnabled) return false;
        ApiCall call = await CallAsync(HttpClientName, "editMessageReplyMarkup",
            new { chat_id = chatId, message_id = messageId, reply_markup = new { inline_keyboard = Array.Empty<object>() } },
            cancellationToken);
        return call.Ok;
    }

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (!IsEnabled) return [];

        ApiCall call = await CallAsync(PollingHttpClientName, "getUpdates",
            new { offset, timeout = timeoutSeconds, allowed_updates = AllowedUpdates },
            cancellationToken);

        if (!call.Ok)
            throw new InvalidOperationException(call.Description ?? "getUpdates failed");

        if (call.Result.ValueKind != JsonValueKind.Array) return [];

        List<TelegramUpdate> updates = new(call.Result.GetArrayLength());
        foreach (JsonElement element in call.Result.EnumerateArray())
            updates.Add(TelegramUpdateReader.Read(element));

        return updates;
    }

    public async Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled) return false;
        ApiCall call = await CallAsync(HttpClientName, "deleteWebhook", new { drop_pending_updates = false }, cancellationToken);
        return call.Ok;
    }

    public async Task<bool> SetMyCommandsAsync(IReadOnlyList<TelegramBotCommand> commands, string? languageCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (!IsEnabled) return false;

        ApiCall call = await CallAsync(HttpClientName, "setMyCommands",
            new
            {
                commands = commands.Select(c => new { command = c.Command, description = c.Description }).ToArray(),
                language_code = languageCode,
            },
            cancellationToken);

        return call.Ok;
    }

    /// <summary><c>sendMessage</c> tanasi — <c>reply_markup</c> xom JSON element bo'lib ketadi.</summary>
    private sealed record SendMessageBody(
        [property: System.Text.Json.Serialization.JsonPropertyName("chat_id")] long ChatId,
        [property: System.Text.Json.Serialization.JsonPropertyName("text")] string Text,
        [property: System.Text.Json.Serialization.JsonPropertyName("parse_mode")] string ParseMode,
        [property: System.Text.Json.Serialization.JsonPropertyName("disable_web_page_preview")] bool DisablePreview,
        [property: System.Text.Json.Serialization.JsonPropertyName("reply_markup")] JsonElement? ReplyMarkup);

    private static JsonElement? ParseMarkup(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Telegram javobi: <c>{ok, result}</c> yoki <c>{ok:false, description, parameters.retry_after}</c>.</summary>
    private sealed record ApiCall(bool Ok, JsonElement Result, int StatusCode, string? Description, int? RetryAfterSeconds);

    private async Task<ApiCall> CallAsync(string clientName, string method, object? body, CancellationToken cancellationToken)
    {
        try
        {
            HttpClient client = _httpFactory.CreateClient(clientName);

            // ⚠️ Manzil ABSOLYUT satr sifatida yasaladi: tokenda `:` bor va nisbiy `bot123:ABC/getMe`
            // `bot123` SXEMASI deb o'qilardi (NotSupportedException; Wash'da ham shu xato — `0c05a37`).
            Uri uri = new($"{client.BaseAddress}bot{_token}/{method}");
            using HttpResponseMessage response = await client.PostAsJsonAsync(uri, body ?? new { }, BodyJson, cancellationToken);
            string raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return Read(raw, (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // Best-effort transport — istisno chaqiruvchiga natija sifatida qaytadi.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // Istisno obyekti ATAYLAB logga berilmaydi: xabarida so'rov manzili (token bilan) bo'lishi
            // mumkin. Matni token yashirilgan holda — sababsiz «yiqildi» nosozlikni topishga yordam bermaydi.
            _logger.LogWarning("Telegram '{Method}' chaqiruvi yiqildi: {ErrorType}: {Message}",
                method, ex.GetType().Name, Redact(ex.GetBaseException().Message));
            return new ApiCall(false, default, 0, ex.GetType().Name, null);
        }
    }

    private string Redact(string text) =>
        string.IsNullOrEmpty(_token) ? text : text.Replace(_token, "<token>", StringComparison.Ordinal);

    private static ApiCall Read(string raw, int statusCode)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(raw);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new ApiCall(false, default, statusCode, "unexpected response", null);

            bool ok = root.TryGetProperty("ok", out JsonElement okElement) && okElement.ValueKind == JsonValueKind.True;
            if (!ok)
            {
                string? description = root.TryGetProperty("description", out JsonElement d) ? d.GetString() : null;
                int? retryAfter = root.TryGetProperty("parameters", out JsonElement parameters)
                                  && parameters.TryGetProperty("retry_after", out JsonElement retry)
                                  && retry.TryGetInt32(out int seconds)
                    ? seconds
                    : null;

                return new ApiCall(false, default, statusCode, Truncate(description ?? "rejected"), retryAfter);
            }

            // `result` NUSXALANADI: JsonDocument shu metoddan chiqishda yopiladi.
            return root.TryGetProperty("result", out JsonElement result)
                ? new ApiCall(true, result.Clone(), statusCode, null, null)
                : new ApiCall(true, default, statusCode, null, null);
        }
        catch (JsonException)
        {
            return new ApiCall(false, default, statusCode, Truncate(raw), null);
        }
    }

    private static string Truncate(string text) =>
        text.Length > MaxDescriptionLength ? text[..MaxDescriptionLength] : text;
}
