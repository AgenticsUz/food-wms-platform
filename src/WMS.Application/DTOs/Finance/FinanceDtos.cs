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
    /// Optional: when omitted, inferred from the current debt sign
    /// (negative balance → we are paying the counterparty).
    public PaymentDirection? Direction { get; set; }
    public string? Note { get; set; }
}

public class PaymentHistoryDto
{
    public Guid Id { get; set; }
    public Guid CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public Guid? TransferId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
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
