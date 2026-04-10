using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Finance;

public class TransactionDto
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public int? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public int? TransferId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string RecordedByUserName { get; set; } = null!;
}

public class CreateTransactionDto
{
    public TransactionType Type { get; set; }
    public int? CounterpartyId { get; set; }
    public int? TransferId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
}

public class DebtDto
{
    public int CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public CounterpartyType CounterpartyType { get; set; }
    public decimal Amount { get; set; }
}

public class CreatePaymentDto
{
    public int CounterpartyId { get; set; }
    public int? TransferId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Note { get; set; }
}

public class PaymentHistoryDto
{
    public int Id { get; set; }
    public int CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public int? TransferId { get; set; }
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
