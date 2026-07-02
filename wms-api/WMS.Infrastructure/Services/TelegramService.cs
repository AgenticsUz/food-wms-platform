using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

/// <summary>
/// Telegram Bot API orqali xabar yuboradi. `Telegram:BotToken` sozlanmagan bo'lsa
/// butunlay no-op — shuning uchun default holatda hech qanday tashqi chaqiruv bo'lmaydi.
/// Foydalanuvchi o'z chat_id sini sozlamalarda ulaydi (masalan @userinfobot dan oladi).
/// </summary>
public class TelegramService : ITelegramService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string? _token;

    public TelegramService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _token = config["Telegram:BotToken"];
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_token);

    public async Task SendMessageAsync(string chatId, string text)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(chatId)) return;
        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"https://api.telegram.org/bot{_token}/sendMessage";
            await client.PostAsJsonAsync(url, new { chat_id = chatId, text, parse_mode = "HTML" });
        }
        catch
        {
            // Best-effort — Telegram xatosi biznes oqimini buzmasin
        }
    }
}
