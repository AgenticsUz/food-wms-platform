using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// Kontragent bo'yicha yuruvchi qarz balansi. ⚠️ <c>(tenant_id, counterparty_id)</c>
/// bo'yicha NOYOB va <c>xmin</c> bilan qo'riqlanadi (D13): «topilmasa yarat»
/// naqshi SQLite'da parallel so'rovda ikki qator yaratishi mumkin edi.
/// </summary>
public class Debt : TenantEntity
{
    public Guid CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;
    public decimal Amount { get; set; }
}

/// <summary>
/// Kontragent bilan pul oldi-berdisi. ⚠️ Bu — HISOBOT yozuvi, bank tranzaksiyasi EMAS:
/// tizimda bank/1C integratsiyasi yo'q, summa qo'lda kiritiladi (F10 rejasi §A3.2).
/// </summary>
public class PaymentHistory : TenantEntity
{
    public Guid CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Pul qaysi tomonga ketgani — qarz balansi shunga qarab o'zgaradi.</summary>
    /// <remarks>
    /// ⚠️ Ilgari yo'nalish saqlanMASdi: u faqat qarz balansiga qo'llanib, yozuvda iz
    /// qoldirmasdi — shuning uchun «bu to'lov qaysi tomonga edi?» degan savolga tarixdan
    /// javob yo'q edi va qaytarish (<see cref="ReversalOfId"/>) ham imkonsiz bo'lardi.
    /// </remarks>
    public PaymentDirection Direction { get; set; } = PaymentDirection.In;

    /// <summary>Haqiqiy to'lov sanasi (foydalanuvchi ko'rsatadi).</summary>
    /// <remarks>
    /// ⚠️ <see cref="PaidAt"/> yozuv YARATILGAN lahza (UTC, tizim qo'yadi) va u
    /// o'zgarmaydi; hisobot va filtrlar esa SHU maydonga qaraydi. Ilgari ikkalasi bitta
    /// maydon edi va «kecha to'lagan edi» ni tizim yoza olmasdi.
    /// </remarks>
    public DateTime DocumentDate { get; set; }

    public DateTime PaidAt { get; set; }
    public string? Note { get; set; }

    /// <summary>Qaysi yuzadan kirgan (AI kiritgan yozuvni ajratish uchun).</summary>
    public DocumentSource Source { get; set; } = DocumentSource.Ui;

    /// <summary>
    /// Bu yozuv qaysi to'lovni QAYTARADI (storno). <see langword="null"/> — oddiy to'lov.
    /// </summary>
    /// <remarks>
    /// To'lov O'CHIRILMAYDI: xato summa tarixda qoladi va uning yonida teskari yozuv
    /// turadi — moliyaviy tarix hech qachon «jimgina» o'zgarmasin. Bir to'lovni ikki
    /// marta qaytarib bo'lmaydi: <c>(tenant_id, reversal_of_id)</c> NOYOB.
    /// </remarks>
    public Guid? ReversalOfId { get; set; }

    /// <summary>Qaytarilgan asl to'lov.</summary>
    public PaymentHistory? ReversalOf { get; set; }

    /// <summary>Qaytarish sababi (storno yozuvida to'ldiriladi).</summary>
    public string? ReversalReason { get; set; }

    public Guid RecordedByUserId { get; set; }
    public UserProfile RecordedByUser { get; set; } = null!;
}

public class Transaction : TenantEntity
{
    public TransactionType Type { get; set; }
    public Guid? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public Guid RecordedByUserId { get; set; }
    public UserProfile RecordedByUser { get; set; } = null!;
}
