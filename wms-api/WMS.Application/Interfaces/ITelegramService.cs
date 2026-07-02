namespace WMS.Application.Interfaces;

/// Telegram bot orqali xabar yuborish. Token sozlanmagan bo'lsa (default) — no-op.
public interface ITelegramService
{
    bool IsEnabled { get; }
    Task SendMessageAsync(string chatId, string text);
}
