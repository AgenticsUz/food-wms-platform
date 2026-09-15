using WMS.Application.DTOs.Pricing;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

/// <summary>
/// Narx taklifi (P2.2): «bu mahsulotni oxirgi marta qanchaga sotgan/olgan edik?».
/// </summary>
/// <remarks>
/// <para>
/// Servis narxni QO'YMAYDI — faqat taklif qiladi. Hujjatdagi haqiqiy narxni odam (yoki
/// AI qoralamasini tasdiqlagan odam) kiritadi: oxirgi narx kelishuv emas, esga solish.
/// </para>
/// <para>
/// ⚠️ Tenant parametri YO'Q (CLAUDE.md §3.1) — izolyatsiya RLS va global filtrda.
/// </para>
/// </remarks>
public interface IPricingService
{
    /// <summary>
    /// Oxirgi TASDIQLANGAN hujjatdagi narx: avval shu kontragent bilan, topilmasa umuman.
    /// </summary>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="counterpartyId">
    /// Kontragent; berilsa — avval AYNAN shu kontragent bilan bo'lgan hujjatlar qaraladi
    /// (u bilan kelishilgan narx umumiy narxdan ustun), topilmasa umumiy oxirgi narx.
    /// <see langword="null"/> — to'g'ridan-to'g'ri umumiy.
    /// </param>
    /// <param name="type">
    /// Hujjat turi: <see cref="TransferType.Outgoing"/> — SOTUV narxi,
    /// <see cref="TransferType.Incoming"/> — KIRIM narxi.
    /// ⚠️ Ikkalasi aralashmaydi: sotuv narxini kirimga taklif qilish tannarxni sotuv
    /// darajasiga ko'tarib, foydani nolga tushirardi (va aksincha).
    /// </param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>
    /// Narx va uning manbasi; mos hujjat bo'lmasa <see langword="null"/> — chaqiruvchi
    /// «taklif yo'q» holatini o'zi hal qiladi (nol narx TAKLIF EMAS).
    /// </returns>
    Task<LastPriceDto?> GetLastPriceAsync(
        Guid productId,
        Guid? counterpartyId,
        TransferType type,
        CancellationToken cancellationToken = default);
}
