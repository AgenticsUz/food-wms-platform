using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Catalog;

/// <summary>
/// Nom bo'yicha taxminiy qidiruv — Postgres <c>pg_trgm</c> ustida (P2.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nega <c>word_similarity</c>, <c>similarity</c> emas.</b> <c>similarity</c> ikki
/// satrning TO'LIQ trigramma to'plamini solishtiradi, ya'ni uzun nom qisqa so'rovni
/// «suyultiradi»: «plombir» ↔ «plombir vanilli shokoladli katta paket» bali 0,3 dan
/// pastga tushib ketadi va to'g'ri mahsulot yo'qoladi. <c>word_similarity(so'rov, nom)</c>
/// esa nomning eng mos SO'Z BO'LAGINI oladi — «plombir» uchun uchala plombir ham 1,0
/// beradi, «snikers» ↔ «snickers» esa 0,55 (harf tushib qolgani uchun).
/// </para>
/// <para>
/// ⚠️ So'rov ham, ustun ham AYNAN bitta normalizatordan o'tgan bo'ladi
/// (<see cref="SearchNormalizer"/>) — «Сникерс» va «snickers» bir alifboda uchrashadi.
/// </para>
/// <para>
/// ⚠️ GIN indeks (<c>gin_trgm_ops</c>) bu so'rovda ATAYLAB majburlanmagan: indeksli
/// <c>&lt;%</c> operatori seans chegarasiga (<c>pg_trgm.word_similarity_threshold</c>,
/// sukut 0,6) bo'ysunadi va «snikers» ↔ «snickers» (0,55) ni KESIB tashlar edi.
/// Chegara bizniki bo'lgani uchun filtr ifoda bo'yicha ketadi; indeks katalog kattalashsa
/// (yoki keyinchalik chegara seansda qo'yilsa) foydali bo'ladi.
/// </para>
/// </remarks>
public class SearchService : ISearchService
{
    /// <summary>
    /// Nomzod hisoblanish uchun eng past bal.
    /// </summary>
    /// <remarks>
    /// 0,4 — o'lchangan qiymatlar orasidagi bo'shliqdan: «snikers» ↔ «snickers» 0,55,
    /// «snikers» ↔ «plombir» 0,0 atrofida. Pastroq chegara (0,3) butunlay begona nomlarni
    /// ham nomzod qilib qo'shar, AI qatlami esa har safar «qaysi biri?» deb so'rayverardi.
    /// </remarks>
    public const double MinimumScore = 0.4;

    /// <summary>Chaqiruvchi juda katta <c>limit</c> so'rasa ham shundan oshmaydi.</summary>
    private const int MaxLimit = 100;

    private readonly WmsDbContext _db;

    /// <summary>Kontekstni oladi.</summary>
    /// <param name="db">WMS konteksti (tenant — RLS va global filtrda).</param>
    public SearchService(WmsDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductSearchCandidate>> FindProductsAsync(
        string query, int limit = ISearchService.DefaultLimit, CancellationToken cancellationToken = default)
    {
        if (!TryPrepare(query, limit, out string normalized, out int take))
        {
            return [];
        }

        // ⚠️ Bal avval ANONIM turga chiqariladi, keyin filtr/tartib: record KONSTRUKTORLI
        // proyeksiya ustiga `Where`/`OrderBy` qo'shilsa EF uni tarjima qila olmaydi
        // («client projection» dan keyin kompozitsiya yo'q), anonim tur esa muammosiz.
        // Tekshirilgan SQL: `WHERE … word_similarity(@q, p.name_search) >= 0.4
        // ORDER BY word_similarity(@q, p.name_search) DESC, p.name LIMIT @p`.
        var rows = await _db.Products.AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Barcode,
                Score = EF.Functions.TrigramsWordSimilarity(normalized, p.NameSearch),
            })
            .Where(x => x.Score >= MinimumScore)
            // Teng ballda nom bo'yicha — tartib barqaror bo'lsin (sahifalash va testlar uchun).
            .OrderByDescending(x => x.Score).ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(x => new ProductSearchCandidate(x.Id, x.Name, x.Score, x.Barcode))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CounterpartySearchCandidate>> FindCounterpartiesAsync(
        string query, int limit = ISearchService.DefaultLimit, CancellationToken cancellationToken = default)
    {
        if (!TryPrepare(query, limit, out string normalized, out int take))
        {
            return [];
        }

        var rows = await _db.Counterparties.AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                Score = EF.Functions.TrigramsWordSimilarity(normalized, c.NameSearch),
            })
            .Where(x => x.Score >= MinimumScore)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(x => new CounterpartySearchCandidate(x.Id, x.Name, x.Score, x.Type))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WarehouseSearchCandidate>> FindWarehousesAsync(
        string query, int limit = ISearchService.DefaultLimit, CancellationToken cancellationToken = default)
    {
        if (!TryPrepare(query, limit, out string normalized, out int take))
        {
            return [];
        }

        var rows = await _db.Warehouses.AsNoTracking()
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Type,
                Score = EF.Functions.TrigramsWordSimilarity(normalized, w.NameSearch),
            })
            .Where(x => x.Score >= MinimumScore)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(x => new WarehouseSearchCandidate(x.Id, x.Name, x.Score, x.Type))];
    }

    /// <summary>
    /// So'rovni normallashtiradi va chegaralarni tekshiradi.
    /// </summary>
    /// <param name="query">Xom so'rov.</param>
    /// <param name="limit">So'ralgan chegara.</param>
    /// <param name="normalized">Normallashtirilgan so'rov.</param>
    /// <param name="take">Amaldagi chegara.</param>
    /// <returns>
    /// <see langword="false"/> — qidirishning ma'nosi yo'q (bo'sh yoki faqat tinish belgisi):
    /// bo'sh so'rov bilan <c>word_similarity</c> hamma qatorga 0 beradi, ya'ni butun
    /// katalogni bejiz o'qib chiqqan bo'lardik.
    /// </returns>
    private static bool TryPrepare(string query, int limit, out string normalized, out int take)
    {
        normalized = SearchNormalizer.Normalize(query);
        take = Math.Clamp(limit, 1, MaxLimit);
        return normalized.Length > 0;
    }
}
