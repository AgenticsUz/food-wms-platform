using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Operations;

/// <summary>
/// Telegram Bot API orqali xabar yuboradi. `Telegram:BotToken` sozlanmagan bo'lsa
/// butunlay no-op — shuning uchun default holatda hech qanday tashqi chaqiruv bo'lmaydi.
/// Foydalanuvchi o'z chat_id sini profilda ulaydi (<c>PUT /api/me/telegram</c>; masalan @userinfobot dan oladi).
/// </summary>
/// <remarks>
/// Singleton: token konfiguratsiyadan bir marta o'qiladi va «sozlanmagan» xabari logga BIR MARTA
/// tushadi (har bildirishnomada emas). Token sozlanmaganligi xato EMAS — Telegram ixtiyoriy kanal.
/// </remarks>
public sealed class TelegramService : ITelegramService
{
    /// <summary>
    /// Nomlangan HttpClient. ⚠️ Uning loggerlari O'CHIRILGAN (<c>OperationsModule</c>): IHttpClientFactory
    /// so'rov URI'sini logga yozadi, Bot API'da esa token aynan URI ichida (<c>/bot{token}/...</c>).
    /// </summary>
    public const string HttpClientName = "telegram";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<TelegramService> _logger;
    private readonly string? _token;

    public TelegramService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<TelegramService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _token = config["Telegram:BotToken"]?.Trim();

        if (!IsEnabled)
            _logger.LogInformation("Telegram:BotToken sozlanmagan — bildirishnomalar Telegram'ga yuborilmaydi");
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_token);

    public async Task SendMessageAsync(string chatId, string text)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(chatId)) return;
        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(
                $"bot{_token}/sendMessage", new { chat_id = chatId, text, parse_mode = "HTML" });

            // Noto'g'ri chat id yoki bloklangan bot — foydalanuvchining sozlamasi; log yetarli.
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Telegram xabarni qabul qilmadi: HTTP {StatusCode}", (int)response.StatusCode);
        }
#pragma warning disable CA1031 // Best-effort — Telegram xatosi biznes oqimini buzmasin.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // Istisno obyekti ATAYLAB logga berilmaydi: xabarida so'rov manzili (token bilan) bo'lishi mumkin.
            _logger.LogWarning("Telegram xabari yuborilmadi: {ErrorType}", ex.GetType().Name);
        }
    }
}
