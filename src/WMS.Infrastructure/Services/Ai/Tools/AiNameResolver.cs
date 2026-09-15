using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Nom bo'yicha yechishning natijasi.</summary>
/// <typeparam name="T">Nomzod turi.</typeparam>
/// <param name="Match">Yagona aniq nomzod; <see langword="null"/> — yo'q yoki noaniq.</param>
/// <param name="Candidates">Topilgan nomzodlar (noaniq holatda modelga ko'rsatiladi).</param>
public readonly record struct NameResolution<T>(T? Match, IReadOnlyList<T> Candidates)
    where T : SearchCandidate
{
    /// <summary>Hech narsa topilmadi.</summary>
    public bool NotFound => Candidates.Count == 0;

    /// <summary>Bir nechta nomzod — savol berish kerak (F10 §0.7).</summary>
    public bool IsAmbiguous => Match is null && Candidates.Count > 0;
}

/// <summary>
/// Model yozgan NOMNI yozuvga aylantiradi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Qoida: bitta nomzod — ishlatiladi, bir nechtasi — SAVOL</b> (F10 §0.7). Eng baland
/// balni «g'olib» deb olish eng katta xavf: «Plombir» uch xil mahsulotga to'g'ri keladi va
/// model jimgina birinchisini tanlasa, foydalanuvchi BOSHQA mahsulotning qoldig'ini ko'rib,
/// buni bilmasdi ham. Shuning uchun tanlov modelga emas, ODAMGA qaytariladi.
/// </para>
/// <para>
/// Bal bo'yicha «yetarlicha ustun» degan yumshoq qoida ATAYLAB yo'q: <c>word_similarity</c>
/// bali nomning uzunligiga qarab suzadi va chegarani qayerga qo'ysak ham, u qachondir
/// noto'g'ri tomonga og'ardi. Ikki nomzod — ikki nomzod.
/// </para>
/// </remarks>
internal static class AiNameResolver
{
    /// <summary>Ko'pi bilan nechta nomzod ko'rsatiladi.</summary>
    /// <remarks>Uzun ro'yxat savolni javobsiz qoldiradi: odam 20 ta variantdan tanlamaydi.</remarks>
    public const int MaxCandidates = 5;

    public static NameResolution<T> Resolve<T>(IReadOnlyList<T> candidates)
        where T : SearchCandidate
    {
        if (candidates.Count == 1)
        {
            return new NameResolution<T>(candidates[0], candidates);
        }

        return new NameResolution<T>(null, [.. candidates.Take(MaxCandidates)]);
    }

    /// <summary>Noaniqlik matni — model shu ro'yxatni foydalanuvchiga qaytaradi.</summary>
    /// <typeparam name="T">Nomzod turi.</typeparam>
    /// <param name="query">Foydalanuvchi yozgan nom.</param>
    /// <param name="candidates">Nomzodlar.</param>
    /// <param name="describe">Nomzodni qatorga aylantiruvchi.</param>
    /// <returns>Modelga ketadigan matn.</returns>
    public static string AmbiguousText<T>(string query, IReadOnlyList<T> candidates, Func<T, string> describe)
        where T : SearchCandidate =>
        $"'{query}' bo'yicha {candidates.Count} ta nomzod topildi — qaysi biri kerakligini FOYDALANUVCHIDAN so'rang:\n"
        + string.Join("\n", candidates.Select(c => "- " + describe(c)));

    /// <summary>Topilmadi matni.</summary>
    /// <param name="query">Foydalanuvchi yozgan nom.</param>
    /// <param name="what">Nima izlangani («mahsulot», «kontragent»).</param>
    /// <returns>Modelga ketadigan matn.</returns>
    public static string NotFoundText(string query, string what) =>
        $"'{query}' bo'yicha {what} topilmadi. Nomni boshqacha yozib ko'rishni taklif qiling.";
}
