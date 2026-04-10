using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class FinanceService : IFinanceService
{
    private readonly WmsDbContext _db;
    public FinanceService(WmsDbContext db) => _db = db;

    public async Task<List<TransactionDto>> GetTransactionsAsync(int tenantId, TransactionType? type,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        var q = _db.Transactions.Where(t => t.TenantId == tenantId)
            .Include(t => t.Counterparty).Include(t => t.RecordedByUser).AsQueryable();

        if (type.HasValue) q = q.Where(t => t.Type == type.Value);
        if (from.HasValue) q = q.Where(t => t.Date >= from.Value);
        if (to.HasValue) q = q.Where(t => t.Date <= to.Value);

        return await q.OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id, Type = t.Type, CounterpartyId = t.CounterpartyId,
                CounterpartyName = t.Counterparty != null ? t.Counterparty.Name : null,
                TransferId = t.TransferId, Amount = t.Amount,
                Description = t.Description, Date = t.Date,
                RecordedByUserName = t.RecordedByUser.FullName
            }).ToListAsync();
    }

    public async Task<TransactionDto> CreateTransactionAsync(int tenantId, int userId, CreateTransactionDto dto)
    {
        var tx = new Transaction
        {
            TenantId = tenantId, Type = dto.Type, CounterpartyId = dto.CounterpartyId,
            TransferId = dto.TransferId, Amount = dto.Amount,
            Description = dto.Description, Date = dto.Date, RecordedByUserId = userId
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        var result = await _db.Transactions
            .Include(t => t.Counterparty).Include(t => t.RecordedByUser)
            .FirstAsync(t => t.Id == tx.Id);

        return new TransactionDto
        {
            Id = result.Id, Type = result.Type, CounterpartyId = result.CounterpartyId,
            CounterpartyName = result.Counterparty?.Name, TransferId = result.TransferId,
            Amount = result.Amount, Description = result.Description, Date = result.Date,
            RecordedByUserName = result.RecordedByUser.FullName
        };
    }

    public async Task<List<DebtDto>> GetDebtsAsync(int tenantId)
    {
        return await _db.Debts.Where(d => d.TenantId == tenantId)
            .Include(d => d.Counterparty)
            .Select(d => new DebtDto
            {
                CounterpartyId = d.CounterpartyId, CounterpartyName = d.Counterparty.Name,
                CounterpartyType = d.Counterparty.Type, Amount = d.Amount
            }).ToListAsync();
    }

    public async Task<PaymentHistoryDto> CreatePaymentAsync(int tenantId, int userId, CreatePaymentDto dto)
    {
        var payment = new PaymentHistory
        {
            TenantId = tenantId, CounterpartyId = dto.CounterpartyId,
            TransferId = dto.TransferId, Amount = dto.Amount,
            Method = dto.Method, PaidAt = DateTime.UtcNow,
            Note = dto.Note, RecordedByUserId = userId
        };
        _db.PaymentHistories.Add(payment);

        // Update debt
        var debt = await _db.Debts.FirstOrDefaultAsync(d =>
            d.TenantId == tenantId && d.CounterpartyId == dto.CounterpartyId);
        if (debt != null)
        {
            // Payment reduces absolute debt
            if (debt.Amount > 0)
                debt.Amount -= dto.Amount; // they owed us, now paying
            else
                debt.Amount += dto.Amount; // we owed them, now paying
        }

        await _db.SaveChangesAsync();

        var result = await _db.PaymentHistories
            .Include(p => p.Counterparty).Include(p => p.RecordedByUser)
            .FirstAsync(p => p.Id == payment.Id);

        return new PaymentHistoryDto
        {
            Id = result.Id, CounterpartyId = result.CounterpartyId,
            CounterpartyName = result.Counterparty.Name, TransferId = result.TransferId,
            Amount = result.Amount, Method = result.Method, PaidAt = result.PaidAt,
            Note = result.Note, RecordedByUserName = result.RecordedByUser.FullName
        };
    }

    public async Task<FinanceSummaryDto> GetSummaryAsync(int tenantId)
    {
        var income = await _db.Transactions
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var expense = await _db.Transactions
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var debt = await _db.Debts.Where(d => d.TenantId == tenantId)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        return new FinanceSummaryDto
        {
            TotalIncome = income, TotalExpense = expense,
            TotalDebt = debt, NetProfit = income - expense
        };
    }
}
