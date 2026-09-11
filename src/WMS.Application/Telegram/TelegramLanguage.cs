using WMS.Application.Common.Localization;

namespace WMS.Application.Telegram;

/// <summary>Bot tili: Telegram <c>language_code</c> dan <c>uz</c>/<c>ru</c>; boshqasi — sozlamadagi sukut.</summary>
public static class TelegramLanguage
{
    public static string Resolve(string? languageCode, string fallback)
    {
        string? normalized = Lang.Normalize(languageCode);
        return normalized is Lang.Uz or Lang.Ru ? normalized : (Lang.Normalize(fallback) ?? Lang.Default);
    }
}
