using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Finance;

public class TransactionDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public Guid? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public Guid? TransferId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string RecordedByUserName { get; set; } = null!;
}

public class CreateTransactionDto
{
    public TransactionType Type { get; set; }
    public Guid? CounterpartyId { get; set; }
    public Guid? TransferId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
}

public class DebtDto
{
    public Guid CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public CounterpartyType CounterpartyType { get; set; }
    public decimal Amount { get; set; }
}

public class CreatePaymentDto
{
    public Guid CounterpartyId { get; set; }
    public Guid? TransferId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Pul yo'nalishi. Bo'sh bo'lsa joriy qarz belgisidan chiqariladi.</summary>
    /// <remarks>
    /// ⚠️ Taxmin faqat balans NOLDAN FARQLI bo'lganda ishlaydi: nol balansda «kim kimga»
    /// degan savolga javob yo'q va noto'g'ri tanlov summani teskari tomonga yozib, xatoni
    /// IKKI barobar qilardi. Shunday holatda servis 400 qaytaradi va yo'nalishni so'raydi.
    /// AI qatlami (F10·A3.2) esa yo'nalishni HAR DOIM oshkora yuboradi.
    /// </remarks>
    public PaymentDirection? Direction { get; set; }

    /// <summary>To'lov sanasi; bo'sh bo'lsa — bugun. Orqaga sana <c>documents.backdate</c> bilan.</summary>
    public DateTime? DocumentDate { get; set; }

    public string? Note { get; set; }
}

/// <summary>To'lovni qaytarish (storno) so'rovi.</summary>
public class ReversePaymentDto
{
    /// <summary>Sabab — tarixda ko'rinadi, majburiy.</summary>
    public string Reason { get; set; } = null!;
}

public class PaymentHistoryDto
{
    public Guid Id { get; set; }
    public Guid CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public Guid? TransferId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Pul yo'nalishi (In — bizga tushdi, Out — biz to'ladik).</summary>
    public PaymentDirection Direction { get; set; }

    /// <summary>Hujjat sanasi — hisobot shu bo'yicha.</summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>Yozuv yaratilgan lahza (audit izi).</summary>
    public DateTime PaidAt { get; set; }

    /// <summary>Qaysi yuzadan kirgan.</summary>
    public DocumentSource Source { get; set; }

    /// <summary>Bu yozuv qaytargan to'lov (storno bo'lsa).</summary>
    public Guid? ReversalOfId { get; set; }

    /// <summary>Qaytarish sababi.</summary>
    public string? ReversalReason { get; set; }

    /// <summary>Shu to'lov keyinchalik qaytarilganmi.</summary>
    public bool IsReversed { get; set; }

    public string? Note { get; set; }
    public string RecordedByUserName { get; set; } = null!;
}

public class FinanceSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal NetProfit { get; set; }
}
