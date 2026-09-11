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

    public static string Help(string lang) =>
        Translations.Format(HelpKey, lang) + "\n\n" + Translations.Format(CommandsKey, lang);

    public const string CommandsKey = "Commands: /bugun — today, /kutilmoqda — pending transfers, /qoldiq <name> — stock, /muddat — expiring batches, /qarz — receivables, /status — where you are connected, /stop — disconnect.";
    public const string NotConnectedKey = "You are not connected to any organization. Open your WMS profile → Connect Telegram.";
    public const string ConnectedListKey = "Connected organizations:";
    public const string MutedCountKey = "{0} notification type(s) muted";
    public const string StoppedKey = "Disconnected from all organizations. To reconnect, open your WMS profile.";

    /// <summary>Ulanish qatori: tenant nomi, profil ismi, o'chirilgan turlar soni.</summary>
    public sealed record ConnectionLine(string TenantName, string FullName, int MutedCount);

    public static string Status(IReadOnlyList<ConnectionLine> lines, string lang)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0) return Translations.Format(NotConnectedKey, lang);

        var sb = new System.Text.StringBuilder(Translations.Format(ConnectedListKey, lang));
        foreach (ConnectionLine line in lines)
        {
            sb.Append("\n🏭 <b>").Append(WebUtility.HtmlEncode(line.TenantName)).Append("</b> — ")
                .Append(WebUtility.HtmlEncode(line.FullName));
            if (line.MutedCount > 0)
                sb.Append(" · ").Append(Translations.Format(MutedCountKey, lang, line.MutedCount));
        }

        return sb.ToString();
    }

    public static string Stopped(string lang) => Translations.Format(StoppedKey, lang);
}
