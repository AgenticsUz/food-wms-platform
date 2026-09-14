using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

/// <summary>
/// Nom bo'yicha «taxminiy» qidiruv (P2.1): xato yozilgan, kirill yoki lotin — farqi yo'q.
/// </summary>
/// <remarks>
/// <para>
/// Nomzodlar O'XSHASHLIK BALI bilan qaytadi va JAVOB BERMAYDI — tanlovni chaqiruvchi
/// qiladi: UI ro'yxatni ko'rsatadi, AI qatlami (A1 <c>find_product</c>) esa bir nechta
/// yaqin nomzod bo'lsa qayta so'raydi. Shuning uchun servis «eng yaxshisi» ni emas,
/// RO'YXATNI qaytaradi.
/// </para>
/// <para>
/// ⚠️ Tenant parametri YO'Q (CLAUDE.md §3.1) — izolyatsiya RLS va global filtrda.
/// </para>
/// </remarks>
public interface ISearchService
{
    /// <summary>Sukut bo'yicha nechta nomzod qaytariladi.</summary>
    public const int DefaultLimit = 10;

    /// <summary>Mahsulot nomzodlari.</summary>
    /// <param name="query">Foydalanuvchi yozgan matn (normallashtirish ichkarida).</param>
    /// <param name="limit">Ko'pi bilan nechta nomzod.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Bal bo'yicha kamayish tartibidagi nomzodlar; mos kelmasa — bo'sh ro'yxat.</returns>
    Task<IReadOnlyList<ProductSearchCandidate>> FindProductsAsync(
        string query, int limit = DefaultLimit, CancellationToken cancellationToken = default);

    /// <summary>Kontragent nomzodlari.</summary>
    /// <param name="query">Foydalanuvchi yozgan matn.</param>
    /// <param name="limit">Ko'pi bilan nechta nomzod.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Bal bo'yicha kamayish tartibidagi nomzodlar.</returns>
    Task<IReadOnlyList<CounterpartySearchCandidate>> FindCounterpartiesAsync(
        string query, int limit = DefaultLimit, CancellationToken cancellationToken = default);

    /// <summary>Ombor nomzodlari.</summary>
    /// <param name="query">Foydalanuvchi yozgan matn.</param>
    /// <param name="limit">Ko'pi bilan nechta nomzod.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Bal bo'yicha kamayish tartibidagi nomzodlar.</returns>
    Task<IReadOnlyList<WarehouseSearchCandidate>> FindWarehousesAsync(
        string query, int limit = DefaultLimit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Qidiruv nomzodi — kim topildi va QANCHALIK ishonch bilan.
/// </summary>
/// <param name="Id">Yozuv identifikatori.</param>
/// <param name="Name">Ko'rsatiladigan nom (normallashtirilgani EMAS, asl nom).</param>
/// <param name="Score">0..1 o'xshashlik bali; 1 — so'rov nomning ichida aynan bor.</param>
public abstract record SearchCandidate(Guid Id, string Name, double Score);

/// <summary>Mahsulot nomzodi.</summary>
/// <param name="Id">Mahsulot identifikatori.</param>
/// <param name="Name">Mahsulot nomi.</param>
/// <param name="Score">O'xshashlik bali.</param>
/// <param name="Barcode">Shtrix-kod — bir xil nomli ikki qatorni ajratish uchun ko'rsatiladi.</param>
public sealed record ProductSearchCandidate(Guid Id, string Name, double Score, string? Barcode)
    : SearchCandidate(Id, Name, Score);

/// <summary>Kontragent nomzodi.</summary>
/// <param name="Id">Kontragent identifikatori.</param>
/// <param name="Name">Kontragent nomi.</param>
/// <param name="Score">O'xshashlik bali.</param>
/// <param name="Type">Mijoz/ta'minotchi — bir xil nom ikki rolda bo'lishi mumkin.</param>
public sealed record CounterpartySearchCandidate(Guid Id, string Name, double Score, CounterpartyType Type)
    : SearchCandidate(Id, Name, Score);

/// <summary>Ombor nomzodi.</summary>
/// <param name="Id">Ombor identifikatori.</param>
/// <param name="Name">Ombor nomi.</param>
/// <param name="Score">O'xshashlik bali.</param>
/// <param name="Type">Ombor turi (xom ashyo / tayyor mahsulot).</param>
public sealed record WarehouseSearchCandidate(Guid Id, string Name, double Score, WarehouseType Type)
    : SearchCandidate(Id, Name, Score);
