using System.Net;
using WMS.Application.Common.Localization;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Botning javob matnlari (HTML). Kalitlar <see cref="Translations"/> da (uz/ru) — bu bot
/// interfeysining bir qismi, tahrirlanadigan shablon emas (Wash qarori).
/// </summary>
public static class TelegramBotReplies
{
    public const string LinkedKey = "✅ Connected to <b>{0}</b>. Notifications will arrive here.";
    public const string LinkExpiredKey = "This link has expired or was already used. Open your WMS profile and request a new one.";
    public const string HelpKey = "To receive notifications, connect this chat from WMS: Settings → Profile → Connect Telegram.";

    /// <summary>Tenant nomi HTML'ga kodlanadi — <c>&lt;</c> yoki <c>&amp;</c> bo'lsa Telegram butun xabarni rad etardi.</summary>
    public static string Linked(string tenantName, string lang) =>
        Translations.Format(LinkedKey, lang, WebUtility.HtmlEncode(tenantName));

    public static string LinkExpired(string lang) => Translations.Format(LinkExpiredKey, lang);

    public static string Help(string lang) => Translations.Format(HelpKey, lang);
}
