using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Pricing;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

/// <summary>
/// Oxirgi narx taklifi (P2.2) — tasdiqlangan hujjat qatorlari ustidan.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nega faqat <see cref="TransferStatus.Confirmed"/>.</b> Kutayotgan hujjat — hali
/// qabul qilinmagan taklif: u rad etilishi yoki narxi tuzatilishi mumkin. Tasdiqlanmagan
/// qatordan narx taklif qilinsa, xato kiritilgan raqam keyingi hujjatlarga ko'chib,
/// o'z-o'zini tasdiqlaydigan zanjir hosil bo'lardi.
/// </para>
/// <para>
/// <b>Nega tartib <c>DocumentDate</c> bo'yicha.</b> «Oxirgi narx» — tovar HAQIQATDA
/// ketgan kunning narxi. <c>CreatedAt</c> bo'yicha tartiblansa, kechagi hujjatni bugun
/// kiritish uni «eng oxirgi» qilib qo'yardi (P2.3 sababi).
/// </para>
/// <para>
/// ⚠️ Bir kunda bir nechta hujjat bo'lishi odatiy (<c>DocumentDate</c> — kun boshi),
/// shuning uchun tartib <c>Number</c> bilan to'ldiriladi: hisoblagich o'sib boradi, ya'ni
/// katta raqam — kechroq kiritilgan hujjat. Oxirgi tayanch — <c>Id</c> (v7, monoton):
/// tartib HAR DOIM bir xil javob bersin, aks holda bir xil so'rov ikki xil narx qaytarardi.
/// </para>
/// </remarks>
public class PricingService : IPricingService
{
    private readonly WmsDbContext _db;

    /// <summary>Kontekstni oladi.</summary>
    /// <param name="db">WMS konteksti (tenant — RLS va global filtrda).</param>
    public PricingService(WmsDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<LastPriceDto?> GetLastPriceAsync(
        Guid productId,
        Guid? counterpartyId,
        TransferType type,
        CancellationToken cancellationToken = default)
    {
        // Nol narxli qator ATAYLAB tashlanadi: «oxirgi narx — 0» taklif emas, u formadagi
        // maydonni jimgina nolga tushirib, xato hujjat yozilishiga olib kelardi. Bunday
        // qatorlar bor (namuna, hadya, narxi keyin kiritiladigan hujjat).
        IQueryable<TransferItem> candidates = _db.TransferItems
            .Where(i => i.ProductId == productId
                && i.UnitPrice > 0m
                && i.Transfer.Type == type
                && i.Transfer.Status == TransferStatus.Confirmed);

        if (counterpartyId is { } id)
        {
            LastPriceDto? own = await PickLatestAsync(
                candidates.Where(i => i.Transfer.CounterpartyId == id), cancellationToken);

            if (own is not null)
            {
                own.IsSameCounterparty = true;
                return own;
            }
        }

        LastPriceDto? any = await PickLatestAsync(candidates, cancellationToken);
        if (any is not null)
        {
            // Umumiy so'rov shu kontragentga tushishi mumkin emas (yuqoridagi tor so'rov
            // bo'sh qaytdi), lekin bayroq qatordan hisoblanadi — shart o'zgarsa ham yolg'on
            // aytmasin.
            any.IsSameCounterparty = counterpartyId is { } asked && any.CounterpartyId == asked;
        }

        return any;
    }

    /// <summary>Berilgan qatorlardan eng oxirgisini DTO ga o'giradi.</summary>
    /// <param name="items">Filtrlangan hujjat qatorlari.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Eng oxirgi qator yoki <see langword="null"/>.</returns>
    private static Task<LastPriceDto?> PickLatestAsync(
        IQueryable<TransferItem> items, CancellationToken cancellationToken) =>
        items
            .OrderByDescending(i => i.Transfer.DocumentDate)
            .ThenByDescending(i => i.Transfer.Number)
            .ThenByDescending(i => i.TransferId)
            .ThenByDescending(i => i.Id)
            .Select(i => new LastPriceDto
            {
                UnitPrice = i.UnitPrice,
                DocumentDate = i.Transfer.DocumentDate,
                TransferId = i.TransferId,
                Number = i.Transfer.Number,
                CounterpartyId = i.Transfer.CounterpartyId,

                // Kontragent yumshoq o'chirilgan bo'lsa navigatsiya global filtrdan o'tib
                // null qaytadi — narx esa baribir haqiqiy, shuning uchun qator tashlanmaydi.
                CounterpartyName = i.Transfer.Counterparty == null ? null : i.Transfer.Counterparty.Name,
            })
            .FirstOrDefaultAsync(cancellationToken);
}
