using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Transfers;

public class TransferDto
{
    public Guid Id { get; set; }
    public TransferType Type { get; set; }
    public TransferStatus Status { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public Guid? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public decimal? CommissionPercent { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public ReturnReason? ReturnReason { get; set; }
    public string? ReturnReasonName { get; set; }
    public Guid? OriginalTransferId { get; set; }
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Hujjat sanasi — tovar HAQIQATDA kelgan/ketgan kun (P2.3).</summary>
    /// <remarks>
    /// ⚠️ <see cref="CreatedAt"/> bilan aralashtirilmaydi: u yozuv kiritilgan LAHZA (audit izi),
    /// ro'yxat va hisobotlar esa shu maydonga qaraydi.
    /// </remarks>
    public DateTime DocumentDate { get; set; }

    /// <summary>Tenant ichidagi qisqa hujjat raqami — ekranda «#12» (P2.4).</summary>
    public int Number { get; set; }

    /// <summary>Hujjat qaysi yuzadan kirgan (ekran, Telegram, AI).</summary>
    public DocumentSource Source { get; set; }

    public List<TransferItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class TransferItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public Guid? BatchId { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>Sotilgan partiya tannarxi — hujjat kartasida ko'rsatish uchun (P2.5).</summary>
    /// <remarks>
    /// ⚠️ Chiqim TASDIQLANGANDA FEFO tanlagan partiyadan yozilgan NUSXA
    /// (<c>TransferItem.UnitCost</c>), mahsulotning bugungi tannarxi EMAS.
    /// <see langword="null"/> — tannarx noma'lum (tasdiqlanmagan hujjat, kirim qatori yoki
    /// backfill qilinmagan eski yozuv); yuzada u NOL deb ko'rsatilmasligi kerak, aks holda
    /// foyda «sof» bo'lib ko'rinadi (sabab <c>ProductProfitDto</c> izohida).
    /// </remarks>
    public decimal? UnitCost { get; set; }
}

public class CreateTransferDto
{
    /// <summary>Hujjat sanasi; berilmasa — bugun (<c>DocumentDates.Resolve</c> qoidasi, P2.3).</summary>
    /// <remarks>
    /// ⚠️ <c>Source</c> ataylab YO'Q: mijoz o'z hujjatini «AI yozgan» deb ko'rsatolmasin —
    /// manbani servis metodining parametri belgilaydi, yuk tarkibi emas.
    /// </remarks>
    public DateTime? DocumentDate { get; set; }

    public TransferType Type { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Guid? CounterpartyId { get; set; }
    public Guid? AgentId { get; set; }
    public decimal? CommissionPercent { get; set; }
    public ReturnReason? ReturnReason { get; set; }
    public Guid? OriginalTransferId { get; set; }
    public string? Note { get; set; }
    public List<CreateTransferItemDto> Items { get; set; } = new();
}

public class CreateTransferItemDto
{
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? LocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
