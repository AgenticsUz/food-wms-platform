using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

public class FinanceService : IFinanceService
{
    private readonly WmsDbContext _db;
    private readonly ITelegramPartnerNotifier _partners;
    private readonly ICurrentUser _user;

    public FinanceService(WmsDbContext db, ITelegramPartnerNotifier partners, ICurrentUser user)
    {
        _db = db;
        _partners = partners;
        _user = user;
    }

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
    public async Task<PaymentHistoryDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto,
        DocumentSource source = DocumentSource.Ui)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");
        await ValidateReferencesAsync(dto.CounterpartyId, dto.TransferId);

        // ⚠️ Sana AVVAL tekshiriladi: ruxsatsiz orqaga sana bo'lsa qarz qatori ham
        // yaratilmasin (bo'sh qator qolib ketardi).
        DateTime documentDate = DocumentDates.Resolve(dto.DocumentDate, _user);

        // Qarz qatori to'lovdan OLDIN kafolatlanadi (change tracker hali toza) — sababi DebtLedger'da.
        var debt = await DebtLedger.GetOrCreateAsync(_db, dto.CounterpartyId);

        PaymentDirection direction = ResolveDirection(dto.Direction, debt.Amount);

        var payment = new PaymentHistory
        {
            CounterpartyId = dto.CounterpartyId,
            TransferId = dto.TransferId, Amount = dto.Amount,
            Method = dto.Method, Direction = direction,
            DocumentDate = documentDate, PaidAt = DateTime.UtcNow,
            Source = source,
            Note = dto.Note, RecordedByUserId = userId
        };
        _db.PaymentHistories.Add(payment);

        debt.Amount += DebtDelta(direction, dto.Amount);

        await _db.SaveChangesAsync();

        // Mijozga (TG13): to'lov qabul qilindi va qoldiq balans — tenant ruxsat bergan bo'lsa.
        if (direction == PaymentDirection.In)
        {
            try
            {
                await _partners.NotifyClientAsync(dto.CounterpartyId, NotificationMessages.ClientPaymentReceived,
                    [NotificationMessages.Amount(dto.Amount), NotificationMessages.Amount(Math.Max(debt.Amount, 0))]);
            }
#pragma warning disable CA1031 // Best-effort — to'lov saqlangan.
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            { Console.Error.WriteLine($"[Finance] telegram notify failed: {ex.GetType().Name}"); }
        }

        var result = await _db.PaymentHistories.AsNoTracking()
            .Include(p => p.Counterparty).Include(p => p.RecordedByUser)
            .FirstAsync(p => p.Id == payment.Id);

        return ToDto(result, isReversed: false);
    }

    /// <inheritdoc />
    public async Task<PaymentHistoryDto> ReversePaymentAsync(Guid userId, Guid paymentId, string reason,
        DocumentSource source = DocumentSource.Ui)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        PaymentHistory original = await _db.PaymentHistories.FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new NotFoundException("Payment not found");

        // Storno - moliyaviy TARIX; uni yana qaytarish zanjirni tushunib bo'lmas qilardi
        // («qaytarishning qaytarishi» = asl to'lov, lekin uchta yozuv bilan).
        if (original.ReversalOfId is not null)
        {
            throw new AppException("A reversal cannot be reversed");
        }

        if (await _db.PaymentHistories.AnyAsync(p => p.ReversalOfId == paymentId))
        {
            throw new AppException("This payment is already reversed");
        }

        Debt debt = await DebtLedger.GetOrCreateAsync(_db, original.CounterpartyId);
        PaymentDirection reverseDirection = Opposite(original.Direction);

        PaymentHistory reversal = new()
        {
            CounterpartyId = original.CounterpartyId,
            TransferId = original.TransferId,
            Amount = original.Amount,
            Method = original.Method,
            Direction = reverseDirection,

            // Storno BUGUNGI hujjat: kechagi kunning yopilgan hisoboti o'zgarmaydi,
            // tuzatish esa o'z kunida ko'rinadi.
            DocumentDate = DateTime.UtcNow.Date,
            PaidAt = DateTime.UtcNow,
            Source = source,
            ReversalOfId = original.Id,
            ReversalReason = reason,
            RecordedByUserId = userId,
        };

        _db.PaymentHistories.Add(reversal);
        debt.Amount += DebtDelta(reverseDirection, original.Amount);

        // Bitta `SaveChanges`: qarz `xmin` bilan, storno esa `(tenant_id, reversal_of_id)`
        // noyob indeksi bilan qo'riqlanadi - parallel ikki qaytarishdan biri 409 oladi.
        await _db.SaveChangesAsync();

        // Mijozga xabar: to'lov qabul qilingani haqida allaqachon xabar ketgan bo'lishi mumkin,
        // ya'ni jimgina tuzatish unda NOTO'G'RI balans qoldirardi.
        if (original.Direction == PaymentDirection.In)
        {
            try
            {
                await _partners.NotifyClientAsync(original.CounterpartyId, NotificationMessages.ClientPaymentReversed,
                    [NotificationMessages.Amount(original.Amount), NotificationMessages.Amount(Math.Max(debt.Amount, 0))]);
            }
#pragma warning disable CA1031 // Best-effort - storno saqlangan.
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            { Console.Error.WriteLine($"[Finance] telegram notify failed: {ex.GetType().Name}"); }
        }

        PaymentHistory saved = await _db.PaymentHistories.AsNoTracking()
            .Include(p => p.Counterparty).Include(p => p.RecordedByUser)
            .FirstAsync(p => p.Id == reversal.Id);

        return ToDto(saved, isReversed: false);
    }

    /// <summary>Yo'nalishni aniqlaydi; nol balansda taxmin QILINMAYDI.</summary>
    /// <param name="requested">So'ralgan yo'nalish (ixtiyoriy).</param>
    /// <param name="balance">Joriy qarz balansi.</param>
    /// <returns>Yo'nalish.</returns>
    /// <remarks>
    /// Musbat balans - kontragent bizga qarzdor (tushum), manfiy - biz unga. Nol balansda
    /// belgi hech narsa demaydi: o'sha yerda taxmin qilish summani teskari tomonga yozib,
    /// xatoni IKKI barobar qilardi (tuzatish uchun ikki hissa kerak bo'lardi).
    /// </remarks>
    private static PaymentDirection ResolveDirection(PaymentDirection? requested, decimal balance)
    {
        if (requested is { } value)
        {
            return value;
        }

        if (balance == 0m)
        {
            throw new AppException("Payment direction is required when the balance is zero");
        }

        return balance < 0 ? PaymentDirection.Out : PaymentDirection.In;
    }

    /// <summary>Qarz balansiga qo'shiladigan o'zgarish.</summary>
    /// <param name="direction">Yo'nalish.</param>
    /// <param name="amount">Summa.</param>
    /// <returns>Balans o'zgarishi.</returns>
    private static decimal DebtDelta(PaymentDirection direction, decimal amount) =>
        direction == PaymentDirection.In ? -amount : amount;

    /// <summary>Teskari yo'nalish.</summary>
    /// <param name="direction">Yo'nalish.</param>
    /// <returns>Qarama-qarshi yo'nalish.</returns>
    private static PaymentDirection Opposite(PaymentDirection direction) =>
        direction == PaymentDirection.In ? PaymentDirection.Out : PaymentDirection.In;

    private static PaymentHistoryDto ToDto(PaymentHistory p, bool isReversed) => new()
    {
        Id = p.Id,
        CounterpartyId = p.CounterpartyId,
        CounterpartyName = p.Counterparty.Name,
        TransferId = p.TransferId,
        Amount = p.Amount,
        Method = p.Method,
        Direction = p.Direction,
        DocumentDate = p.DocumentDate,
        PaidAt = p.PaidAt,
        Source = p.Source,
        ReversalOfId = p.ReversalOfId,
        ReversalReason = p.ReversalReason,
        IsReversed = isReversed,
        Note = p.Note,
        RecordedByUserName = p.RecordedByUser.FullName,
    };

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid? counterpartyId, int page, int pageSize)
    {
        var q = _db.PaymentHistories.AsQueryable();

        if (counterpartyId.HasValue) q = q.Where(p => p.CounterpartyId == counterpartyId.Value);

        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        // Tartib hujjat sanasi bo'yicha (`PaidAt` - audit izi); teng sanalarda yozuv
        // vaqti, keyin kalit - ro'yxat har safar bir xil chiqsin.
        return await q.OrderByDescending(p => p.DocumentDate).ThenByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PaymentHistoryDto
            {
                Id = p.Id, CounterpartyId = p.CounterpartyId,
                CounterpartyName = p.Counterparty.Name, TransferId = p.TransferId,
                Amount = p.Amount, Method = p.Method, Direction = p.Direction,
                DocumentDate = p.DocumentDate, PaidAt = p.PaidAt, Source = p.Source,
                ReversalOfId = p.ReversalOfId, ReversalReason = p.ReversalReason,

                // «Qaytarilganmi» ro'yxatda darhol ko'rinsin, aks holda bekor qilingan
                // yozuv hamon haqiqiy to'lovdek ko'rinardi.
                IsReversed = _db.PaymentHistories.Any(r => r.ReversalOfId == p.Id),
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
