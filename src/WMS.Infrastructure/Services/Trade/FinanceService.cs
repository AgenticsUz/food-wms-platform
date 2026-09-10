using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

public class FinanceService : IFinanceService
{
    private readonly WmsDbContext _db;
    public FinanceService(WmsDbContext db) => _db = db;

    public async Task<List<TransactionDto>> GetTransactionsAsync(TransactionType? type,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        var q = _db.Transactions.AsQueryable();

        if (type.HasValue) q = q.Where(t => t.Type == type.Value);
        if (from.HasValue) q = q.Where(t => t.Date >= from.Value);
        if (to.HasValue) q = q.Where(t => t.Date <= to.Value);

        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        return await q.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
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

    public async Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto dto)
    {
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");
        await ValidateReferencesAsync(dto.CounterpartyId, dto.TransferId);

        var tx = new Transaction
        {
            Type = dto.Type, CounterpartyId = dto.CounterpartyId,
            TransferId = dto.TransferId, Amount = dto.Amount,
            Description = dto.Description, Date = dto.Date, RecordedByUserId = userId
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        var result = await _db.Transactions.AsNoTracking()
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

    public async Task DeleteTransactionAsync(Guid id)
    {
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("Transaction not found");
        tx.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<DebtDto>> GetDebtsAsync()
    {
        return await _db.Debts
            .OrderBy(d => d.Counterparty.Name)
            .Select(d => new DebtDto
            {
                CounterpartyId = d.CounterpartyId, CounterpartyName = d.Counterparty.Name,
                CounterpartyType = d.Counterparty.Type, Amount = d.Amount
            }).ToListAsync();
    }

    /// <remarks>
    /// D13: to'lov yozuvi va qarz o'zgarishi BITTA <c>SaveChangesAsync</c> da; qarz qatori
    /// <c>xmin</c> bilan qo'riqlanadi — bitta kontragentga parallel ikki to'lovdan ikkinchisi 409
    /// oladi (SQLite davrida biri ikkinchisining o'zgarishini jimgina ustidan yozardi).
    /// </remarks>
    public async Task<PaymentHistoryDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto)
    {
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");
        await ValidateReferencesAsync(dto.CounterpartyId, dto.TransferId);

        // Qarz qatori to'lovdan OLDIN kafolatlanadi (change tracker hali toza) — sababi DebtLedger'da.
        var debt = await DebtLedger.GetOrCreateAsync(_db, dto.CounterpartyId);

        var payment = new PaymentHistory
        {
            CounterpartyId = dto.CounterpartyId,
            TransferId = dto.TransferId, Amount = dto.Amount,
            Method = dto.Method, PaidAt = DateTime.UtcNow,
            Note = dto.Note, RecordedByUserId = userId
        };
        _db.PaymentHistories.Add(payment);

        // Without an explicit direction, fall back to the debt sign: a negative
        // balance means we owe the counterparty, so the payment is going out.
        var direction = dto.Direction
            ?? (debt.Amount < 0 ? PaymentDirection.Out : PaymentDirection.In);
        debt.Amount += direction == PaymentDirection.In ? -dto.Amount : dto.Amount;

        await _db.SaveChangesAsync();

        var result = await _db.PaymentHistories.AsNoTracking()
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

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid? counterpartyId, int page, int pageSize)
    {
        var q = _db.PaymentHistories.AsQueryable();

        if (counterpartyId.HasValue) q = q.Where(p => p.CounterpartyId == counterpartyId.Value);

        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        return await q.OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PaymentHistoryDto
            {
                Id = p.Id, CounterpartyId = p.CounterpartyId,
                CounterpartyName = p.Counterparty.Name, TransferId = p.TransferId,
                Amount = p.Amount, Method = p.Method, PaidAt = p.PaidAt,
                Note = p.Note, RecordedByUserName = p.RecordedByUser.FullName
            }).ToListAsync();
    }

    public async Task<FinanceSummaryDto> GetSummaryAsync()
    {
        // Yig'indilar SQL'da (`numeric`): SQLite davrida pul REAL edi va xotirada yig'ilardi.
        var totals = await _db.Transactions
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();
        var income = totals.Where(t => t.Type == TransactionType.Income).Sum(t => t.Total);
        var expense = totals.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Total);

        var debt = await _db.Debts.SumAsync(d => d.Amount);

        return new FinanceSummaryDto
        {
            TotalIncome = income, TotalExpense = expense,
            TotalDebt = debt, NetProfit = income - expense
        };
    }

    private async Task ValidateReferencesAsync(Guid? counterpartyId, Guid? transferId)
    {
        if (counterpartyId.HasValue &&
            !await _db.Counterparties.AnyAsync(c => c.Id == counterpartyId.Value))
            throw new NotFoundException("Counterparty not found");

        if (transferId.HasValue &&
            !await _db.Transfers.AnyAsync(t => t.Id == transferId.Value))
            throw new NotFoundException("Transfer not found");
    }
}
