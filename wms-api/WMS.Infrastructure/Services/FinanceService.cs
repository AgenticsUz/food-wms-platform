using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
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
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");
        await ValidateReferencesAsync(tenantId, dto.CounterpartyId, dto.TransferId);

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

    public async Task DeleteTransactionAsync(int tenantId, int id)
    {
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId)
            ?? throw new NotFoundException("Transaction not found");
        tx.IsDeleted = true;
        await _db.SaveChangesAsync();
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
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");
        await ValidateReferencesAsync(tenantId, dto.CounterpartyId, dto.TransferId);

        var payment = new PaymentHistory
        {
            TenantId = tenantId, CounterpartyId = dto.CounterpartyId,
            TransferId = dto.TransferId, Amount = dto.Amount,
            Method = dto.Method, PaidAt = DateTime.UtcNow,
            Note = dto.Note, RecordedByUserId = userId
        };
        _db.PaymentHistories.Add(payment);

        var debt = await _db.Debts.FirstOrDefaultAsync(d =>
            d.TenantId == tenantId && d.CounterpartyId == dto.CounterpartyId);
        if (debt == null)
        {
            debt = new Debt { TenantId = tenantId, CounterpartyId = dto.CounterpartyId, Amount = 0 };
            _db.Debts.Add(debt);
        }

        // Without an explicit direction, fall back to the debt sign: a negative
        // balance means we owe the counterparty, so the payment is going out.
        var direction = dto.Direction
            ?? (debt.Amount < 0 ? PaymentDirection.Out : PaymentDirection.In);
        debt.Amount += direction == PaymentDirection.In ? -dto.Amount : dto.Amount;

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

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(int tenantId, int? counterpartyId, int page, int pageSize)
    {
        var q = _db.PaymentHistories.Where(p => p.TenantId == tenantId)
            .Include(p => p.Counterparty).Include(p => p.RecordedByUser).AsQueryable();

        if (counterpartyId.HasValue) q = q.Where(p => p.CounterpartyId == counterpartyId.Value);

        return await q.OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PaymentHistoryDto
            {
                Id = p.Id, CounterpartyId = p.CounterpartyId,
                CounterpartyName = p.Counterparty.Name, TransferId = p.TransferId,
                Amount = p.Amount, Method = p.Method, PaidAt = p.PaidAt,
                Note = p.Note, RecordedByUserName = p.RecordedByUser.FullName
            }).ToListAsync();
    }

    public async Task<FinanceSummaryDto> GetSummaryAsync(int tenantId)
    {
        // Sum in memory: money stays decimal end-to-end (SQLite stores it as REAL,
        // but casting through double in the query loses the decimal contract).
        var amounts = await _db.Transactions
            .Where(t => t.TenantId == tenantId)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync();
        var income = amounts.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = amounts.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var debt = (await _db.Debts.Where(d => d.TenantId == tenantId)
            .Select(d => d.Amount).ToListAsync()).Sum();

        return new FinanceSummaryDto
        {
            TotalIncome = income, TotalExpense = expense,
            TotalDebt = debt, NetProfit = income - expense
        };
    }

    private async Task ValidateReferencesAsync(int tenantId, int? counterpartyId, int? transferId)
    {
        if (counterpartyId.HasValue &&
            !await _db.Counterparties.AnyAsync(c => c.Id == counterpartyId.Value && c.TenantId == tenantId))
            throw new NotFoundException("Counterparty not found");

        if (transferId.HasValue &&
            !await _db.Transfers.AnyAsync(t => t.Id == transferId.Value && t.TenantId == tenantId))
            throw new NotFoundException("Transfer not found");
    }
}
