namespace WMS.Application.Common.Localization;

/// <summary>
/// The languages the API answers in. English is the source language: every message is
/// written in English in the code and looked up for the other two, so an untranslated
/// message degrades to English instead of to a missing-key placeholder.
///
/// The tenant app also offers Uzbek Cyrillic; it maps to <see cref="Uz"/> until a Cyrillic
/// column is added to <see cref="Translations"/> — one dictionary entry per row, no code change.
/// </summary>
public static class Lang
{
    public const string Uz = "uz";
    public const string Ru = "ru";
    public const string En = "en";

    public const string Default = Uz;

    public static readonly string[] Supported = [Uz, Ru, En];

    /// <summary>
    /// Picks a supported language from an Accept-Language header
    /// ("ru-RU,ru;q=0.9,en;q=0.8" → "ru"). Unknown or missing → the default.
    /// Quality values are honoured, so a browser's ordered list works as intended.
    /// </summary>
    public static string FromHeader(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage)) return Default;

        var candidates = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                var pieces = part.Split(';', StringSplitOptions.TrimEntries);
                var tag = pieces[0];
                var quality = 1.0;
                var q = pieces.FirstOrDefault(p => p.StartsWith("q=", StringComparison.OrdinalIgnoreCase));
                if (q != null && double.TryParse(q[2..], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    quality = parsed;
                return (Tag: tag, Quality: quality);
            })
            .OrderByDescending(x => x.Quality);

        foreach (var (tag, _) in candidates)
        {
            var normalized = Normalize(tag);
            if (normalized != null) return normalized;
        }
        return Default;
    }

    /// Maps a language tag onto a supported language: "ru-RU" → "ru", "uz-Cyrl" / "uz-cyrl" → "uz".
    /// Returns null when the tag is not one we speak.
    public static string? Normalize(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;

        var primary = tag.Split('-', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
        return primary switch
        {
            Uz => Uz,
            Ru => Ru,
            En => En,
            _ => null
        };
    }
}
