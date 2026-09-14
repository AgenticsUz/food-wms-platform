using System.Text;

namespace WMS.Application.Common;

/// <summary>
/// Nom qidiruvi uchun normalizator: kirill↔lotin farqini YO'Q qiladi (P2.1).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Nega C# da, nega Postgres <c>unaccent</c> emas.</b> <c>unaccent</c> faqat
/// diakritikani olib tashlaydi («é» → «e»), kirillni lotinga O'GIRMAYDI: «Сникерс»
/// undan «Сникерс» bo'lib chiqadi va «snickers» bilan bitta ham umumiy trigramma
/// bermaydi. Shuning uchun har nom shu yerda lotinga o'giriladi va natija
/// <c>name_search</c> ustuniga yoziladi; GIN trigram indeks AYNAN shu ustunda,
/// so'rov matni ham shu funksiyadan o'tadi — ikkala tomon bir alifboda bo'ladi.
/// </para>
/// <para>
/// ⚠️ Bu TARJIMA emas, ALIFBO: «un» → «мука» kabi sinonimlar ATAYLAB yo'q (REJA §P2.1).
/// Transliteratsiya o'zbek lotin yozuvi qoidasi bo'yicha (ж → j, х → x, ҳ → h, ў → o),
/// ya'ni «Пломбир» → «plombir», «Шоколад» → «shokolad».
/// </para>
/// <para>
/// ⚠️ Migratsiyadagi backfill SQL'i (<c>F10_NameSearch</c>) SHU jadvalning aynan nusxasi —
/// biri o'zgarsa ikkinchisi ham o'zgarishi shart, aks holda eski qatorlar boshqacha
/// normallashgan bo'lib qoladi.
/// </para>
/// </remarks>
public static class SearchNormalizer
{
    /// <summary>
    /// Natija uzunligi chegarasi — <c>name_search</c> ustunining kengligi.
    /// </summary>
    /// <remarks>
    /// Nom 200 belgi, lekin transliteratsiya UZAYTIRADI («щ» → «sh», «ю» → «yu»), shuning
    /// uchun ustun ikki barobar keng olingan va normalizator baribir kesadi.
    /// </remarks>
    public const int MaxLength = 400;

    /// <summary>So'z ichida YO'QOLADIGAN belgilar: o'zbek apostrofi va uning barcha ko'rinishlari.</summary>
    /// <remarks>
    /// «o‘» va «o'» bir xil so'z: apostrof bo'sh joyga aylansa «bo'g'irsoq» uchta so'zga
    /// bo'linib ketardi va trigramma mosligi buzilardi.
    /// </remarks>
    private const string DroppedChars = "'ʻʼ‘’`´";

    /// <summary>
    /// Bir belgidan uzun o'giriladigan kirill harflari.
    /// </summary>
    private static readonly Dictionary<char, string> MultiCharCyrillic = new()
    {
        ['ё'] = "yo", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh",
        ['щ'] = "sh", ['ю'] = "yu", ['я'] = "ya",
    };

    /// <summary>
    /// Bir belgiga o'giriladigan kirill harflari (<c>'\0'</c> — butunlay tashlab yuboriladi).
    /// </summary>
    /// <remarks>
    /// O'zbek kirillining o'ziga xos harflari ham bor: ў → o, ғ → g, қ → q, ҳ → h —
    /// ular lotin yozuvida «o‘», «g‘», «q», «h», apostrof esa baribir tashlanadi.
    /// </remarks>
    private static readonly Dictionary<char, char> SingleCharCyrillic = new()
    {
        ['а'] = 'a', ['б'] = 'b', ['в'] = 'v', ['г'] = 'g', ['ғ'] = 'g',
        ['д'] = 'd', ['е'] = 'e', ['ж'] = 'j', ['з'] = 'z', ['и'] = 'i',
        ['й'] = 'y', ['к'] = 'k', ['қ'] = 'q', ['л'] = 'l', ['м'] = 'm',
        ['н'] = 'n', ['о'] = 'o', ['ў'] = 'o', ['п'] = 'p', ['р'] = 'r',
        ['с'] = 's', ['т'] = 't', ['у'] = 'u', ['ф'] = 'f', ['х'] = 'x',
        ['ҳ'] = 'h', ['ы'] = 'i', ['э'] = 'e', ['ъ'] = '\0', ['ь'] = '\0',
    };

    /// <summary>
    /// Nomni qidiruv shakliga keltiradi: kichik harf + kirilldan lotinga + ortiqcha belgisiz.
    /// </summary>
    /// <param name="text">Mahsulot/kontragent/ombor nomi yoki foydalanuvchi so'rovi.</param>
    /// <returns>Faqat <c>a-z</c>, <c>0-9</c> va bitta bo'sh joydan iborat satr; bo'sh kirishga — bo'sh satr.</returns>
    /// <example>«Сникерс» → «snikers», «Snickers 50 г» → «snickers 50 g», «Пломбир шоколад» → «plombir shokolad».</example>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder builder = new(text.Length + 8);

        foreach (char raw in text)
        {
            char ch = char.ToLowerInvariant(raw);

            if (DroppedChars.Contains(ch, StringComparison.Ordinal))
            {
                continue;
            }

            if (MultiCharCyrillic.TryGetValue(ch, out string? many))
            {
                builder.Append(many);
                continue;
            }

            if (SingleCharCyrillic.TryGetValue(ch, out char one))
            {
                if (one != '\0')
                {
                    builder.Append(one);
                }

                continue;
            }

            if (char.IsAsciiLetterLower(ch) || char.IsAsciiDigit(ch))
            {
                builder.Append(ch);
                continue;
            }

            // Qolgan hamma narsa (tinish belgisi, foiz, boshqa alifbo) — so'z chegarasi.
            // Ataylab tashlab yuborilmaydi: «50%chegirma» ikki so'z bo'lib qolsin.
            builder.Append(' ');
        }

        // Ketma-ket bo'sh joylar bitta bo'ladi — trigrammalar ikki bo'sh joydan buzilmasin.
        string collapsed = string.Join(' ', builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length > MaxLength ? collapsed[..MaxLength] : collapsed;
    }
}
