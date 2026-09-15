using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Transfer : TenantEntity
{
    public TransferType Type { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public Guid? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    /// <summary>Agent orqali sotuv (ixtiyoriy).</summary>
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    /// <summary>Shu sotuv uchun agent foizining override'i.</summary>
    public decimal? CommissionPercent { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public UserProfile? CreatedByUser { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary>Tenant ichidagi qisqa hujjat raqami (1, 2, 3 …).</summary>
    /// <remarks>
    /// Guid — kalit, lekin ODAM uchun emas: ekranda «#a1b2c3d4» ko'rinardi va telefonda
    /// aytib bo'lmasdi. Raqam tenant ichida ketma-ket, BO'SHLIQ bo'lishi mumkin (bekor
    /// qilingan urinish raqamni qaytarmaydi) — bu ataylab: bo'shliqsizlik uchun navbatni
    /// qulflash kerak bo'lardi.
    /// </remarks>
    public int Number { get; set; }

    /// <summary>Hujjat sanasi — tovar HAQIQATDA kelgan/ketgan kun.</summary>
    /// <remarks>
    /// ⚠️ <c>CreatedAt</c> (yozuv yaratilgan lahza) bilan ARALASHTIRILMAYDI: kecha kelgan
    /// kirim bugun kiritilsa, hisobot uni KECHAGI kunda ko'rsatishi kerak. Filtrlar,
    /// hisobotlar va FEFO shu maydonga qaraydi; <c>CreatedAt</c> — faqat audit izi.
    /// Kelajak sana taqiqlanadi, orqaga sana <c>documents.backdate</c> ruxsati bilan.
    /// </remarks>
    public DateTime DocumentDate { get; set; }

    /// <summary>Qaysi yuzadan kirgan (AI kiritgan hujjatni ajratish uchun).</summary>
    public DocumentSource Source { get; set; } = DocumentSource.Ui;

    /// <summary>Qoralamani tayyorlagan AI suhbati (<see cref="Source"/> = <c>Ai</c> da).</summary>
    /// <remarks>
    /// <see cref="Source"/> «AI qatnashgan» deydi, bu ustun esa QAYSI so'rov bilan degan
    /// savolga javob beradi — tekshiruvchi hujjatdan suhbatga o'tib, foydalanuvchi aslida
    /// nima so'raganini ko'radi. Havola qilingan suhbat tarix tozalashda o'chirilmaydi.
    /// </remarks>
    public Guid? AiConversationId { get; set; }

    /// <summary><c>Type == Return</c> bo'lganda.</summary>
    public ReturnReason? ReturnReason { get; set; }

    /// <summary>Qaytarilayotgan sotuv (ixtiyoriy).</summary>
    public Guid? OriginalTransferId { get; set; }

    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public ICollection<TransferItem> Items { get; set; } = new List<TransferItem>();
}

/// <summary>⚠️ F6 da <c>tenant_id</c> qo'shildi (D4) — sabab <see cref="Location"/> da.</summary>
public class TransferItem : TenantEntity
{
    public Guid TransferId { get; set; }
    public Transfer Transfer { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Chiqimda FEFO tanlagan partiyaning tannarxi — NUSXA, hisobot uchun.</summary>
    /// <remarks>
    /// Nusxa olinadi, chunki partiya keyin o'chirilishi yoki tannarxi to'g'rilanishi mumkin,
    /// sotilgan tovar foydasi esa SOTUV LAHZASIDAGI tannarxdan hisoblanishi kerak.
    /// <see langword="null"/> — tannarx noma'lum bo'lgan (yoki chiqim bo'lmagan) qator.
    /// </remarks>
    public decimal? UnitCost { get; set; }
}
