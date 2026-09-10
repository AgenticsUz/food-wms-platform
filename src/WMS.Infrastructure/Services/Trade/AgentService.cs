using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

// ⚠️ F6: agent portali (o'z JWT'si bilan login, profil, o'z hisoboti) o'chdi — D8.
public class AgentService : IAgentService
{
    private readonly WmsDbContext _db;

    public AgentService(WmsDbContext db) => _db = db;

    // ── CRUD ──

    public async Task<List<AgentDto>> GetAllAsync()
    {
        var agents = await _db.Agents.AsNoTracking()
            .OrderByDescending(a => a.IsActive).ThenBy(a => a.Name)
            .ToListAsync();

        var stats = await LoadStatsAsync(null);
        return agents.Select(a => MapToDtoWithStats(a, stats.GetValueOrDefault(a.Id))).ToList();
    }

    public async Task<AgentDto> GetByIdAsync(Guid id)
    {
        var a = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Agent not found");
        var stats = await LoadStatsAsync(id);
        return MapToDtoWithStats(a, stats.GetValueOrDefault(id));
    }

    /// <summary>Qisqa ko'rsatkichlar (bekor qilinganlarsiz) — agent bo'yicha SQL'da yig'iladi.</summary>
    /// <remarks>SQLite davrida tenantning HAMMA komissiya yozuvi xotiraga o'qilib, u yerda yig'ilardi.</remarks>
    private async Task<Dictionary<Guid, CommissionStats>> LoadStatsAsync(Guid? agentId)
    {
        var q = _db.CommissionRecords.Where(c => c.Status != CommissionStatus.Cancelled);
        if (agentId.HasValue) q = q.Where(c => c.AgentId == agentId.Value);

        var rows = await q.GroupBy(c => c.AgentId)
            .Select(g => new
            {
                AgentId = g.Key,
                Count = g.Count(),
                Sales = g.Sum(c => c.SaleAmount),
                Commission = g.Sum(c => c.CommissionAmount),
                Paid = g.Sum(c => c.IsPaid ? c.CommissionAmount : 0m)
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.AgentId, r => new CommissionStats(r.Count, r.Sales, r.Commission, r.Paid));
    }

    private sealed record CommissionStats(int SalesCount, decimal TotalSales, decimal TotalCommission, decimal CommissionPaid);

    private static AgentDto MapToDtoWithStats(Agent a, CommissionStats? stats)
    {
        var dto = MapToDto(a);
        if (stats == null) return dto;

        dto.SalesCount = stats.SalesCount;
        dto.TotalSales = stats.TotalSales;
        dto.TotalCommission = stats.TotalCommission;
        dto.CommissionPaid = stats.CommissionPaid;
        dto.CommissionDue = dto.TotalCommission - dto.CommissionPaid;
        return dto;
    }

    public async Task<AgentDto> CreateAsync(CreateAgentDto dto)
    {
        ValidateCommissionPercent(dto.CommissionPercent);
        var a = new Agent
        {
            Name = dto.Name,
            Phone = PhoneHelper.Normalize(dto.Phone),
            CommissionPercent = dto.CommissionPercent,
            IsActive = dto.IsActive
        };
        _db.Agents.Add(a);
        await _db.SaveChangesAsync();
        return MapToDto(a);
    }

    public async Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto)
    {
        ValidateCommissionPercent(dto.CommissionPercent);
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Agent not found");
        a.Name = dto.Name;
        a.Phone = PhoneHelper.Normalize(dto.Phone);
        a.CommissionPercent = dto.CommissionPercent;
        a.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return MapToDto(a);
    }

    public async Task DeleteAsync(Guid id)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Agent not found");
        a.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private static void ValidateCommissionPercent(decimal percent)
    {
        if (percent is < 0 or > 100)
            throw new AppException("Commission percent must be between 0 and 100");
    }

    // ── Sales report & commissions ──

    public async Task<AgentSalesReportDto> GetSalesReportAsync(Guid agentId, DateTime? from, DateTime? to)
    {
        var a = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == agentId)
            ?? throw new NotFoundException("Agent not found");
        return await BuildReport(a, from, to);
    }

    public async Task<List<CommissionRecordDto>> GetCommissionsAsync(Guid agentId)
    {
        return await _db.CommissionRecords
            .Where(c => c.AgentId == agentId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommissionRecordDto
            {
                Id = c.Id, AgentId = c.AgentId, AgentName = c.Agent.Name,
                TransferId = c.TransferId,
                CounterpartyName = c.Transfer.Counterparty != null ? c.Transfer.Counterparty.Name : null,
                SaleAmount = c.SaleAmount, CommissionPercent = c.CommissionPercent,
                CommissionAmount = c.CommissionAmount, Status = c.Status,
                IsPaid = c.IsPaid, PaidAt = c.PaidAt, CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    /// <remarks>
    /// D13: komissiya yozuvlari <c>xmin</c> bilan qo'riqlanadi (TradeConfiguration). Parallel ikki
    /// to'lov bir xil yozuvlarni «to'landi» qilib, xarajatni IKKI marta yozardi; endi ikkinchisi
    /// 409 oladi va uning xarajat yozuvi ham o'sha bitta <c>SaveChangesAsync</c> bilan qaytadi.
    /// </remarks>
    public async Task PayCommissionAsync(Guid userId, Guid agentId, PayCommissionDto dto)
    {
        var agent = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == agentId)
            ?? throw new NotFoundException("Agent not found");
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");

        // Mark oldest unpaid, non-cancelled commission records as paid, up to the amount.
        var unpaid = await _db.CommissionRecords
            .Where(c => c.AgentId == agentId && !c.IsPaid && c.Status != CommissionStatus.Cancelled)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync();

        var totalDue = unpaid.Sum(c => c.CommissionAmount);
        if (dto.Amount > totalDue + 0.01m)
            throw new AppException("Amount exceeds the outstanding commission ({0:N0})", totalDue);

        var remaining = dto.Amount;
        var now = DateTime.UtcNow;
        foreach (var rec in unpaid)
        {
            if (remaining < rec.CommissionAmount - 0.01m) break; // only settle records fully covered
            rec.IsPaid = true;
            rec.PaidAt = now;
            remaining -= rec.CommissionAmount;
        }

        // Partial payments are not tracked per record, so an amount that does not
        // cover whole records would silently vanish from the books — reject it.
        if (remaining > 0.01m)
        {
            var payable = unpaid.Where(c => !c.IsPaid).Select(c => c.CommissionAmount).ToList();
            throw new AppException(
                "Amount must cover whole commission records; {0:N0} is left uncovered. Next payable record: {1}",
                remaining, payable.Count > 0 ? payable[0].ToString("N0") : "none");
        }

        // Optionally record a finance expense for the payout.
        if (dto.RecordAsExpense)
        {
            _db.Transactions.Add(new Transaction
            {
                Type = TransactionType.Expense,
                Amount = dto.Amount,
                Description = dto.Note ?? $"Agent komissiya to'lovi: {agent.Name}",
                Date = now,
                RecordedByUserId = userId
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateCommissionStatusAsync(Guid recordId, UpdateCommissionStatusDto dto)
    {
        var rec = await _db.CommissionRecords.FirstOrDefaultAsync(c => c.Id == recordId)
            ?? throw new NotFoundException("Commission record not found");
        rec.Status = dto.Status;
        if (dto.Status == CommissionStatus.Cancelled)
        {
            rec.IsPaid = false;
            rec.PaidAt = null;
        }
        await _db.SaveChangesAsync();
    }

    // ── Helpers ──

    /// <remarks>Yig'indilar SQL'da: holat × to'langanlik bo'yicha bir necha qator, kun bo'yicha grafik.</remarks>
    private async Task<AgentSalesReportDto> BuildReport(Agent a, DateTime? from, DateTime? to)
    {
        var q = _db.CommissionRecords.Where(c => c.AgentId == a.Id);
        if (from.HasValue) q = q.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(c => c.CreatedAt <= to.Value);

        var buckets = await q.GroupBy(c => new { c.Status, c.IsPaid })
            .Select(g => new
            {
                g.Key.Status,
                g.Key.IsPaid,
                Count = g.Count(),
                Sales = g.Sum(c => c.SaleAmount),
                Commission = g.Sum(c => c.CommissionAmount)
            })
            .ToListAsync();

        var active = buckets.Where(b => b.Status != CommissionStatus.Cancelled).ToList();

        var report = new AgentSalesReportDto
        {
            AgentId = a.Id,
            AgentName = a.Name,
            CommissionPercent = a.CommissionPercent,
            SalesCount = active.Sum(b => b.Count),
            TotalSales = active.Sum(b => b.Sales),
            TotalCommission = active.Sum(b => b.Commission),
            CommissionConfirmed = active.Where(b => b.Status == CommissionStatus.Confirmed).Sum(b => b.Commission),
            CommissionPending = active.Where(b => b.Status == CommissionStatus.Pending).Sum(b => b.Commission),
            CommissionCancelled = buckets.Where(b => b.Status == CommissionStatus.Cancelled).Sum(b => b.Commission),
            CommissionPaid = active.Where(b => b.IsPaid).Sum(b => b.Commission)
        };
        report.CommissionDue = report.TotalCommission - report.CommissionPaid;

        // Kun — UTC bo'yicha (sana bazada timestamptz, UTC'da saqlanadi).
        report.Timeline = await q
            .Where(c => c.Status != CommissionStatus.Cancelled)
            .GroupBy(c => c.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AgentSalesPointDto
            {
                Date = g.Key,
                SaleAmount = g.Sum(c => c.SaleAmount),
                CommissionAmount = g.Sum(c => c.CommissionAmount)
            })
            .ToListAsync();

        return report;
    }

    private static AgentDto MapToDto(Agent a) => new()
    {
        Id = a.Id, Name = a.Name, Phone = a.Phone,
        CommissionPercent = a.CommissionPercent, IsActive = a.IsActive
    };
}
