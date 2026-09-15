namespace WMS.Application.Common;

/// <summary>
/// <c>Ai</c> bo'limi (env: <c>Ai__ApiKey</c> …). Kalit bo'sh — AI BUTUNLAY o'chiq:
/// yuzalar «sozlanmagan» deydi, hech qanday tashqi chaqiriq bo'lmaydi, qolgan tizim
/// odatdagidek ishlaydi (<c>TelegramOptions</c> naqshi).
/// </summary>
/// <remarks>
/// Dev va prod — BOSHQA kalitlar: bitta kalit ikki muhitda bo'lsa dev tajribasi prod
/// hisobini yeydi va kalitni almashtirish ikkala muhitni birdan uzadi.
/// </remarks>
public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Anthropic API kaliti. Bo'sh — modul o'chiq (xato emas).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Asosiy model.</summary>
    /// <remarks>
    /// Sukut — <c>claude-sonnet-5</c> (F10 rejasi). Sinov to'plami (A1) yetarli ball bermasa
    /// bu yerdan kuchliroq modelga o'tiladi: kod model nomini BILMAYDI, narx jadvali ham
    /// <see cref="Pricing"/> dan keladi.
    /// </remarks>
    public string Model { get; set; } = "claude-sonnet-5";

    /// <summary>Marshrutlovchi arzon model (A5) — hozir ishlatilmaydi.</summary>
    public string? RouterModel { get; set; }

    /// <summary>
    /// O'ylash chuqurligi: <c>low</c> | <c>medium</c> | <c>high</c> | <c>xhigh</c> | <c>max</c>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Sukut ATAYLAB <c>low</c>, provayder sukuti esa <c>high</c> — ya'ni bu qiymat har
    /// so'rovda OSHKORA yuboriladi. WMS savollari («qoldiq qancha?») chuqur o'ylashni talab
    /// qilmaydi; sinov to'plami ballni past ko'rsatsa ko'tarib solishtiriladi.
    /// </remarks>
    public string Effort { get; set; } = "low";

    /// <summary>Bitta javobning chiqish chegarasi.</summary>
    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>Provayder javobini kutish muddati.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// PLATFORMA bo'yicha kunlik xarajat shifti (USD). Oshsa — hamma tenantga
    /// <c>ai_unavailable</c>.
    /// </summary>
    /// <remarks>
    /// Bu — ilova darajasidagi to'r. Undan tashqarida Anthropic Console'ning kalit bo'yicha
    /// sarf limiti turishi kerak: kodda xato bo'lsa ham hisob portlamasin.
    /// </remarks>
    public decimal DailyUsdCap { get; set; } = 10m;

    /// <summary>Suhbat tarixi shuncha kundan keyin tozalanadi.</summary>
    /// <remarks>
    /// Hujjat yoki to'lovdan HAVOLA QILINGAN suhbat tozalashdan chetlab o'tiladi: aks holda
    /// «bu hujjatni AI qanday so'rov bilan tayyorlagan?» degan audit savoli javobsiz qolardi.
    /// Tozalash fon vazifasi — A1; bu yerda faqat siyosat qiymati.
    /// </remarks>
    public int HistoryRetentionDays { get; set; } = 90;

    /// <summary>Model bo'yicha narx jadvali (<c>Ai:Pricing:claude-sonnet-5:…</c>).</summary>
    /// <remarks>
    /// Kodga QOTIRILMAYDI: tarif o'zgarganda deploy emas, konfig o'zgarishi kifoya.
    /// Jadvalda model topilmasa xarajat 0 deb yozilmaydi — <c>AiPricing</c> buni ogohlantirish
    /// bilan belgilaydi (jim nol «AI bepul» degan xato xulosaga olib borardi).
    /// </remarks>
    public Dictionary<string, AiModelPricing> Pricing { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>Bitta modelning 1 million token uchun narxi (USD).</summary>
/// <remarks>
/// To'rt stavka ham kerak: kesh yozish oddiy kirishdan ~1.25× qimmat, kesh o'qish esa ~10×
/// arzon. Ikkitasi bilan hisoblash keshli tizimda xarajatni sezilarli xato ko'rsatardi.
/// </remarks>
public class AiModelPricing
{
    public decimal InputPerMillion { get; set; }
    public decimal OutputPerMillion { get; set; }
    public decimal CacheWritePerMillion { get; set; }
    public decimal CacheReadPerMillion { get; set; }
}
