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

public class PaymentHistory : TenantEntity
{
    public Guid CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Note { get; set; }
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
