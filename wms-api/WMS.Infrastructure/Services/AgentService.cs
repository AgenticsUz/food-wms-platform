using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WMS.Application.Common;
using WMS.Application.DTOs.Agents;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class AgentService : IAgentService
{
    private readonly WmsDbContext _db;
    private readonly IConfiguration _config;

    public AgentService(WmsDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── CRUD ──

    public async Task<List<AgentDto>> GetAllAsync(int tenantId)
    {
        var agents = await _db.Agents
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.IsActive).ThenBy(a => a.Name)
            .ToListAsync();

        var commissions = await _db.CommissionRecords
            .Where(c => c.TenantId == tenantId)
            .ToListAsync();

        return agents.Select(a => MapToDtoWithStats(a,
            commissions.Where(c => c.AgentId == a.Id))).ToList();
    }

    public async Task<AgentDto> GetByIdAsync(int tenantId, int id)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Agent not found");
        var recs = await _db.CommissionRecords
            .Where(c => c.TenantId == tenantId && c.AgentId == id)
            .ToListAsync();
        return MapToDtoWithStats(a, recs);
    }

    private static AgentDto MapToDtoWithStats(Agent a, IEnumerable<CommissionRecord> commissions)
    {
        var recs = commissions.Where(c => c.Status != CommissionStatus.Cancelled).ToList();
        var dto = MapToDto(a);
        dto.SalesCount = recs.Count;
        dto.TotalSales = recs.Sum(c => c.SaleAmount);
        dto.TotalCommission = recs.Sum(c => c.CommissionAmount);
        dto.CommissionPaid = recs.Where(c => c.IsPaid).Sum(c => c.CommissionAmount);
        dto.CommissionDue = dto.TotalCommission - dto.CommissionPaid;
        return dto;
    }

    public async Task<AgentDto> CreateAsync(int tenantId, CreateAgentDto dto)
    {
        ValidateCommissionPercent(dto.CommissionPercent);
        var a = new Agent
        {
            TenantId = tenantId,
            Name = dto.Name,
            Phone = PhoneHelper.Normalize(dto.Phone),
            CommissionPercent = dto.CommissionPercent,
            IsActive = dto.IsActive,
            PortalEnabled = dto.PortalEnabled,
            PortalPhone = PhoneHelper.Normalize(dto.PortalPhone),
            PortalPasswordHash = !string.IsNullOrEmpty(dto.PortalPassword)
                ? BCrypt.Net.BCrypt.HashPassword(dto.PortalPassword) : null
        };
        _db.Agents.Add(a);
        await _db.SaveChangesAsync();
        return MapToDto(a);
    }

    public async Task<AgentDto> UpdateAsync(int tenantId, int id, UpdateAgentDto dto)
    {
        ValidateCommissionPercent(dto.CommissionPercent);
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Agent not found");
        a.Name = dto.Name;
        a.Phone = PhoneHelper.Normalize(dto.Phone);
        a.CommissionPercent = dto.CommissionPercent;
        a.IsActive = dto.IsActive;
        a.PortalEnabled = dto.PortalEnabled;
        a.PortalPhone = PhoneHelper.Normalize(dto.PortalPhone);
        if (!string.IsNullOrEmpty(dto.PortalPassword))
            a.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PortalPassword);
        await _db.SaveChangesAsync();
        return MapToDto(a);
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
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

    public async Task<AgentSalesReportDto> GetSalesReportAsync(int tenantId, int agentId, DateTime? from, DateTime? to)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == agentId && x.TenantId == tenantId)
            ?? throw new NotFoundException("Agent not found");
        return await BuildReport(a, tenantId, from, to);
    }

    public async Task<List<CommissionRecordDto>> GetCommissionsAsync(int tenantId, int agentId)
    {
        return await _db.CommissionRecords
            .Where(c => c.TenantId == tenantId && c.AgentId == agentId)
            .Include(c => c.Agent)
            .Include(c => c.Transfer).ThenInclude(t => t.Counterparty)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapCommission(c))
            .ToListAsync();
    }

    public async Task PayCommissionAsync(int tenantId, int userId, int agentId, PayCommissionDto dto)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(x => x.Id == agentId && x.TenantId == tenantId)
            ?? throw new NotFoundException("Agent not found");
        if (dto.Amount <= 0) throw new AppException("Amount must be greater than zero");

        // Mark oldest unpaid, non-cancelled commission records as paid, up to the amount.
        var unpaid = await _db.CommissionRecords
            .Where(c => c.TenantId == tenantId && c.AgentId == agentId
                && !c.IsPaid && c.Status != CommissionStatus.Cancelled)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var totalDue = unpaid.Sum(c => c.CommissionAmount);
        if (dto.Amount > totalDue + 0.01m)
            throw new AppException($"Amount exceeds the outstanding commission ({totalDue:N0})");

        var remaining = dto.Amount;
        foreach (var rec in unpaid)
        {
            if (remaining < rec.CommissionAmount - 0.01m) break; // only settle records fully covered
            rec.IsPaid = true;
            rec.PaidAt = DateTime.UtcNow;
            remaining -= rec.CommissionAmount;
        }

        // Partial payments are not tracked per record, so an amount that does not
        // cover whole records would silently vanish from the books — reject it.
        if (remaining > 0.01m)
        {
            var payable = unpaid.Where(c => !c.IsPaid).Select(c => c.CommissionAmount).ToList();
            throw new AppException(
                $"Amount must cover whole commission records; {remaining:N0} is left uncovered. " +
                $"Next payable record: {(payable.Count > 0 ? payable[0].ToString("N0") : "none")}");
        }

        // Optionally record a finance expense for the payout.
        if (dto.RecordAsExpense)
        {
            _db.Transactions.Add(new Transaction
            {
                TenantId = tenantId,
                Type = TransactionType.Expense,
                Amount = dto.Amount,
                Description = dto.Note ?? $"Agent komissiya to'lovi: {agent.Name}",
                Date = DateTime.UtcNow,
                RecordedByUserId = userId
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateCommissionStatusAsync(int tenantId, int recordId, UpdateCommissionStatusDto dto)
    {
        var rec = await _db.CommissionRecords
            .FirstOrDefaultAsync(c => c.Id == recordId && c.TenantId == tenantId)
            ?? throw new NotFoundException("Commission record not found");
        rec.Status = dto.Status;
        if (dto.Status == CommissionStatus.Cancelled)
        {
            rec.IsPaid = false;
            rec.PaidAt = null;
        }
        await _db.SaveChangesAsync();
    }

    // ── Agent portal (self-service cabinet) ──

    public async Task<AgentPortalAuthResponseDto> PortalLoginAsync(AgentPortalLoginDto dto)
    {
        var normalizedPhone = PhoneHelper.Normalize(dto.Phone);
        var agent = await _db.Agents
            .FirstOrDefaultAsync(a => a.PortalPhone == normalizedPhone && a.PortalEnabled && a.IsActive)
            ?? throw new AppException("Invalid credentials or portal not enabled");

        if (string.IsNullOrEmpty(agent.PortalPasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, agent.PortalPasswordHash))
            throw new AppException("Invalid credentials");

        var claims = new[]
        {
            new Claim("agentId", agent.Id.ToString()),
            new Claim("tenantId", agent.TenantId.ToString()),
            new Claim("isAgentPortal", "true")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AgentPortalAuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Agent = new AgentPortalProfileDto
            {
                Id = agent.Id, Name = agent.Name, Phone = agent.Phone,
                CommissionPercent = agent.CommissionPercent, TenantId = agent.TenantId
            }
        };
    }

    public async Task<AgentPortalProfileDto> GetPortalProfileAsync(int agentId, int tenantId)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == agentId && x.TenantId == tenantId)
            ?? throw new NotFoundException("Agent not found");
        return new AgentPortalProfileDto
        {
            Id = a.Id, Name = a.Name, Phone = a.Phone,
            CommissionPercent = a.CommissionPercent, TenantId = a.TenantId
        };
    }

    public async Task<AgentSalesReportDto> GetPortalSalesReportAsync(int agentId, int tenantId)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == agentId && x.TenantId == tenantId)
            ?? throw new NotFoundException("Agent not found");
        return await BuildReport(a, tenantId, null, null);
    }

    public async Task<List<CommissionRecordDto>> GetPortalCommissionsAsync(int agentId, int tenantId)
    {
        return await GetCommissionsAsync(tenantId, agentId);
    }

    // ── Helpers ──

    private async Task<AgentSalesReportDto> BuildReport(Agent a, int tenantId, DateTime? from, DateTime? to)
    {
        var q = _db.CommissionRecords
            .Where(c => c.TenantId == tenantId && c.AgentId == a.Id);
        if (from.HasValue) q = q.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(c => c.CreatedAt <= to.Value);
        var all = await q.ToListAsync();

        var active = all.Where(c => c.Status != CommissionStatus.Cancelled).ToList();

        var report = new AgentSalesReportDto
        {
            AgentId = a.Id,
            AgentName = a.Name,
            CommissionPercent = a.CommissionPercent,
            SalesCount = active.Count,
            TotalSales = active.Sum(c => c.SaleAmount),
            TotalCommission = active.Sum(c => c.CommissionAmount),
            CommissionConfirmed = active.Where(c => c.Status == CommissionStatus.Confirmed).Sum(c => c.CommissionAmount),
            CommissionPending = active.Where(c => c.Status == CommissionStatus.Pending).Sum(c => c.CommissionAmount),
            CommissionCancelled = all.Where(c => c.Status == CommissionStatus.Cancelled).Sum(c => c.CommissionAmount),
            CommissionPaid = active.Where(c => c.IsPaid).Sum(c => c.CommissionAmount)
        };
        report.CommissionDue = report.TotalCommission - report.CommissionPaid;

        report.Timeline = active
            .GroupBy(c => c.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AgentSalesPointDto
            {
                Date = g.Key,
                SaleAmount = g.Sum(c => c.SaleAmount),
                CommissionAmount = g.Sum(c => c.CommissionAmount)
            }).ToList();

        return report;
    }

    private static AgentDto MapToDto(Agent a) => new()
    {
        Id = a.Id, Name = a.Name, Phone = a.Phone,
        CommissionPercent = a.CommissionPercent, IsActive = a.IsActive,
        PortalEnabled = a.PortalEnabled, PortalPhone = a.PortalPhone
    };

    private static CommissionRecordDto MapCommission(CommissionRecord c) => new()
    {
        Id = c.Id, AgentId = c.AgentId, AgentName = c.Agent.Name,
        TransferId = c.TransferId, CounterpartyName = c.Transfer.Counterparty != null ? c.Transfer.Counterparty.Name : null,
        SaleAmount = c.SaleAmount, CommissionPercent = c.CommissionPercent,
        CommissionAmount = c.CommissionAmount, Status = c.Status,
        IsPaid = c.IsPaid, PaidAt = c.PaidAt, CreatedAt = c.CreatedAt
    };
}
