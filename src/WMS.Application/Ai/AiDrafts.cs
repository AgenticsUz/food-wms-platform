using WMS.Domain.Enums;

namespace WMS.Application.Ai;

/// <summary>Qoralamadagi bitta qator.</summary>
/// <param name="ProductId">Mahsulot.</param>
/// <param name="ProductName">Nomi (yuzada ko'rsatiladi).</param>
/// <param name="UnitShortName">Birlik.</param>
/// <param name="Quantity">Miqdor — ASOSIY birlikda (qadoq allaqachon ochilgan).</param>
/// <param name="UnitPrice">Birlik narxi.</param>
/// <param name="PriceSource">Narx qayerdan: <c>user</c> | <c>last_price</c> | <c>cost</c> | <c>none</c>.</param>
/// <param name="Available">Chiqimda — shu omborda MAVJUD miqdor; kirimda <see langword="null"/>.</param>
public sealed record AiDraftItem(
    Guid ProductId,
    string ProductName,
    string UnitShortName,
    decimal Quantity,
    decimal UnitPrice,
    string PriceSource,
    decimal? Available)
{
    /// <summary>Qatorning summasi.</summary>
    public decimal Total => Math.Round(Quantity * UnitPrice, 2);

    /// <summary>Qoldiq yetmaydi — yuza buni qizil qilib ko'rsatadi.</summary>
    /// <remarks>
    /// ⚠️ Qoralama BARIBIR qaytariladi: «yetmaydi» — foydalanuvchi ko'rishi kerak bo'lgan
    /// HOLAT, qoralamani tashlab yuborish sababi emas. U miqdorni tuzatib yuborishi mumkin.
    /// </remarks>
    public bool Shortfall => Available is { } available && available < Quantity;
}

/// <summary>
/// Hujjat qoralamasi — HALI YARATILMAGAN hujjat.
/// </summary>
/// <param name="Type">Kirim yoki chiqim.</param>
/// <param name="CounterpartyId">Kontragent.</param>
/// <param name="CounterpartyName">Kontragent nomi.</param>
/// <param name="WarehouseId">Ombor (chiqimda manba, kirimda qabul qiluvchi).</param>
/// <param name="WarehouseName">Ombor nomi.</param>
/// <param name="DocumentDate">Hujjat sanasi.</param>
/// <param name="Items">Qatorlar.</param>
/// <param name="Note">Izoh.</param>
/// <remarks>
/// ⚠️ Bu — TAKLIF, hujjat emas (F10 §0.6). Yozuv faqat odam tugma bosganda yaratiladi va
/// o'shanda ham <c>Pending</c> bo'lib tushadi: tasdiqlash yana bir alohida qadam.
/// </remarks>
public sealed record AiTransferDraft(
    TransferType Type,
    Guid CounterpartyId,
    string CounterpartyName,
    Guid WarehouseId,
    string WarehouseName,
    DateTime DocumentDate,
    IReadOnlyList<AiDraftItem> Items,
    string? Note)
{
    /// <summary>Hujjat summasi.</summary>
    public decimal Total => Items.Sum(i => i.Total);

    /// <summary>Kamida bitta qatorda qoldiq yetmaydi.</summary>
    public bool HasShortfall => Items.Any(i => i.Shortfall);
}

/// <summary>
/// To'lov qoralamasi — HALI YOZILMAGAN yozuv.
/// </summary>
/// <param name="CounterpartyId">Kontragent.</param>
/// <param name="CounterpartyName">Kontragent nomi.</param>
/// <param name="Amount">Summa (musbat).</param>
/// <param name="Direction">Yo'nalish — HECH QACHON taxmin qilinmaydi.</param>
/// <param name="Method">To'lov usuli.</param>
/// <param name="DocumentDate">To'lov sanasi.</param>
/// <param name="BalanceBefore">Joriy qarz balansi.</param>
/// <param name="BalanceAfter">Yozuvdan keyingi balans.</param>
/// <param name="Note">Izoh.</param>
/// <remarks>
/// <para>
/// ⚠️ <b>Balans oldi/keyin SHART.</b> «2 000 000 yozilsinmi?» degan savolga odam
/// balansni ko'rmasdan javob bera olmaydi: summa qarzdan katta bo'lsa yoki yo'nalish
/// teskari bo'lsa, xato aynan shu ikki sondan ko'rinadi.
/// </para>
/// <para>
/// ⚠️ Bu — HISOBOT yozuvi, bank amali EMAS. Tizimda haqiqiy tranzaksiya yo'q va AI
/// «pul o'tdi» demasligi kerak (reja §A3.2 konteksti).
/// </para>
/// </remarks>
public sealed record AiPaymentDraft(
    Guid CounterpartyId,
    string CounterpartyName,
    decimal Amount,
    PaymentDirection Direction,
    PaymentMethod Method,
    DateTime DocumentDate,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Note);
