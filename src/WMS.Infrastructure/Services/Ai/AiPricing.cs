using WMS.Application.Ai;
using WMS.Application.Common;

namespace WMS.Infrastructure.Services.Ai;

/// <summary>
/// Token hisobini dollarga aylantiradi (<c>Ai:Pricing:{model}</c>).
/// </summary>
/// <remarks>
/// ⚠️ Narx KODDA emas, konfigda: tarif o'zgarganda deploy kerak bo'lmasin. Shuning uchun
/// «model topilmadi» holati ham bor va u JIM NOL bilan o'tkazilmaydi — nol xarajat
/// hisobotda «AI bepul» degan xato xulosaga olib borardi.
/// </remarks>
internal static class AiPricing
{
    private const decimal Million = 1_000_000m;

    /// <summary>Xarajatni hisoblaydi.</summary>
    /// <param name="options">AI sozlamalari.</param>
    /// <param name="model">Model nomi.</param>
    /// <param name="usage">Token hisobi.</param>
    /// <param name="cost">Hisoblangan xarajat (USD); narx topilmasa 0.</param>
    /// <returns>Narx jadvalida model topildimi.</returns>
    public static bool TryCost(AiOptions options, string model, LlmUsage usage, out decimal cost)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(usage);

        cost = 0m;
        if (string.IsNullOrWhiteSpace(model) || !options.Pricing.TryGetValue(model, out AiModelPricing? price))
        {
            return false;
        }

        cost =
            ((usage.InputTokens * price.InputPerMillion)
            + (usage.OutputTokens * price.OutputPerMillion)
            + (usage.CacheWriteTokens * price.CacheWritePerMillion)
            + (usage.CacheReadTokens * price.CacheReadPerMillion))
            / Million;

        return true;
    }
}
