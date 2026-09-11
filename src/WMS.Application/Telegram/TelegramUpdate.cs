using System.Text.Json;

namespace WMS.Application.Telegram;

/// <summary>Bot nima bilan chaqirilgani.</summary>
public enum TelegramUpdateKind
{
    /// <summary>Tanilmagan yoki e'tiborsiz yangilanish (stiker, rasm, tahrir …).</summary>
    Unknown = 0,

    /// <summary><c>/start</c> — payload bilan yoki usiz.</summary>
    Start = 1,

    /// <summary>Boshqa buyruq (<c>/help</c>, <c>/stop</c> …) — <see cref="TelegramUpdate.Command"/> da nomi.</summary>
    Command = 2,

    /// <summary>Oddiy matn.</summary>
    Text = 3,

    /// <summary>Bot bloklandi yoki chatdan chiqarildi (<c>my_chat_member</c>).</summary>
    Blocked = 4,

    /// <summary>Bloklash bekor qilindi — foydalanuvchi qayta <c>/start</c> qiladi, hech narsa qilinmaydi.</summary>
    Unblocked = 5,
}

/// <summary>
/// Telegram yangilanishining BIZGA kerakli qismi (Wash <c>TelegramUpdate</c> naqshi).
/// </summary>
/// <param name="UpdateId"><c>getUpdates</c> offset'i shundan hisoblanadi.</param>
/// <param name="IsPrivate">Shaxsiy chat; guruh yangilanishlari (TG16 gacha) e'tiborsiz.</param>
/// <param name="LanguageCode">Telegram interfeys tili, xom (<c>ru</c>, <c>uz</c>, <c>en-US</c> …).</param>
/// <param name="Command">Buyruq nomi <c>/</c> siz va <c>@bot</c> siz (<c>start</c>, <c>help</c>).</param>
/// <param name="Payload">Buyruqdan keyingi matn (<c>/start &lt;token&gt;</c> dagi token).</param>
public sealed record TelegramUpdate(
    long UpdateId,
    TelegramUpdateKind Kind,
    long ChatId,
    bool IsPrivate,
    long? FromId,
    string? Username,
    string? FirstName,
    string? LanguageCode,
    string? Command,
    string? Payload,
    string? Text);

/// <summary>
/// Telegram JSON'ini o'qiydi — SOF funksiya, tarmoqqa tegmaydi.
/// </summary>
/// <remarks>
/// Ataylab «kutilgan maydonlar bo'lmasa <see cref="TelegramUpdateKind.Unknown"/>»: bot ochiq
/// internetdan har xil yangilanish oladi va tanimagani uchun istisno tashlash polling'ni
/// to'xtatib, o'sha yangilanishni qayta-qayta o'qishga majbur qilardi (Wash izohi).
/// </remarks>
public static class TelegramUpdateReader
{
    public static TelegramUpdate Read(JsonElement update)
    {
        long updateId = update.ValueKind == JsonValueKind.Object
                        && update.TryGetProperty("update_id", out JsonElement idElement)
                        && idElement.TryGetInt64(out long parsedId)
            ? parsedId
            : 0;

        if (TryMessage(update, out JsonElement message))
        {
            return ReadMessage(updateId, message);
        }

        return ReadChatMember(updateId, update)
               ?? new TelegramUpdate(updateId, TelegramUpdateKind.Unknown, 0, false, null, null, null, null, null, null, null);
    }

    private static TelegramUpdate ReadMessage(long updateId, JsonElement message)
    {
        (long chatId, bool isPrivate) = Chat(message);
        if (chatId == 0)
        {
            return new TelegramUpdate(updateId, TelegramUpdateKind.Unknown, 0, false, null, null, null, null, null, null, null);
        }

        (long? fromId, string? username, string? firstName, string? lang) = From(message);

        string? text = message.TryGetProperty("text", out JsonElement textElement) ? textElement.GetString() : null;

        if (text is not null && text.StartsWith('/'))
        {
            // `/start abc` → command=start, payload=abc; `/help@AgenticsWmsBot` → help (guruhda shunday keladi).
            int space = text.IndexOf(' ', StringComparison.Ordinal);
            string head = space < 0 ? text[1..] : text[1..space];
            int at = head.IndexOf('@', StringComparison.Ordinal);
            string command = (at < 0 ? head : head[..at]).Trim().ToLowerInvariant();
            string? payload = space < 0 ? null : text[(space + 1)..].Trim();
            payload = string.IsNullOrEmpty(payload) ? null : payload;

            TelegramUpdateKind kind = command == "start" ? TelegramUpdateKind.Start : TelegramUpdateKind.Command;
            return new TelegramUpdate(updateId, kind, chatId, isPrivate, fromId, username, firstName, lang, command, payload, text);
        }

        return new TelegramUpdate(
            updateId,
            text is null ? TelegramUpdateKind.Unknown : TelegramUpdateKind.Text,
            chatId, isPrivate, fromId, username, firstName, lang, null, null, text);
    }

    private static bool TryMessage(JsonElement update, out JsonElement message)
    {
        if (update.ValueKind == JsonValueKind.Object
            && update.TryGetProperty("message", out message)
            && message.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        message = default;
        return false;
    }

    /// <summary><c>my_chat_member</c>: <c>kicked</c>/<c>left</c> — bloklandi; <c>member</c> — qaytdi.</summary>
    private static TelegramUpdate? ReadChatMember(long updateId, JsonElement update)
    {
        if (update.ValueKind != JsonValueKind.Object
            || !update.TryGetProperty("my_chat_member", out JsonElement member)
            || !member.TryGetProperty("new_chat_member", out JsonElement status)
            || !status.TryGetProperty("status", out JsonElement state))
        {
            return null;
        }

        TelegramUpdateKind kind = state.GetString() switch
        {
            "kicked" or "left" => TelegramUpdateKind.Blocked,
            "member" => TelegramUpdateKind.Unblocked,
            _ => TelegramUpdateKind.Unknown,
        };

        (long chatId, bool isPrivate) = Chat(member);
        return new TelegramUpdate(updateId, kind, chatId, isPrivate, null, null, null, null, null, null, null);
    }

    private static (long Id, bool IsPrivate) Chat(JsonElement container)
    {
        if (!container.TryGetProperty("chat", out JsonElement chat)
            || !chat.TryGetProperty("id", out JsonElement id)
            || !id.TryGetInt64(out long chatId))
        {
            return (0, false);
        }

        bool isPrivate = chat.TryGetProperty("type", out JsonElement type)
                         && string.Equals(type.GetString(), "private", StringComparison.Ordinal);
        return (chatId, isPrivate);
    }

    private static (long? Id, string? Username, string? FirstName, string? Lang) From(JsonElement message)
    {
        if (!message.TryGetProperty("from", out JsonElement from))
        {
            return (null, null, null, null);
        }

        long? id = from.TryGetProperty("id", out JsonElement idElement) && idElement.TryGetInt64(out long value) ? value : null;
        string? username = from.TryGetProperty("username", out JsonElement name) ? name.GetString() : null;
        string? firstName = from.TryGetProperty("first_name", out JsonElement first) ? first.GetString() : null;
        string? lang = from.TryGetProperty("language_code", out JsonElement code) ? code.GetString() : null;
        return (id, username, firstName, lang);
    }
}
