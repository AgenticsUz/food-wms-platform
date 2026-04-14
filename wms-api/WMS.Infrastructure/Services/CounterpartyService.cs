using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class CounterpartyService : ICounterpartyService
{
    private readonly WmsDbContext _db;
    public CounterpartyService(WmsDbContext db) => _db = db;

    public async Task<List<CounterpartyDto>> GetAllAsync(int tenantId, CounterpartyType? type = null)
    {
        var q = _db.Counterparties.Where(c => c.TenantId == tenantId);
        if (type.HasValue) q = q.Where(c => c.Type == type.Value);

        return await q.Select(c => new CounterpartyDto
        {
            Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
            Address = c.Address, Note = c.Note, PortalEnabled = c.PortalEnabled,
            PortalPhone = c.PortalPhone
        }).ToListAsync();
    }

    public async Task<CounterpartyDto> CreateAsync(int tenantId, CreateCounterpartyDto dto)
    {
        var c = new Counterparty
        {
            TenantId = tenantId, Name = dto.Name, Type = dto.Type, Phone = PhoneHelper.Normalize(dto.Phone),
            Address = dto.Address, Note = dto.Note, PortalEnabled = dto.PortalEnabled,
            PortalPhone = PhoneHelper.Normalize(dto.PortalPhone),
            PortalPasswordHash = !string.IsNullOrEmpty(dto.PortalPassword)
                ? BCrypt.Net.BCrypt.HashPassword(dto.PortalPassword) : null
        };
        _db.Counterparties.Add(c);
        await _db.SaveChangesAsync();
        return MapToDto(c);
    }

    public async Task<CounterpartyDto> UpdateAsync(int tenantId, int id, UpdateCounterpartyDto dto)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new Exception("Counterparty not found");
        c.Name = dto.Name; c.Type = dto.Type; c.Phone = PhoneHelper.Normalize(dto.Phone);
        c.Address = dto.Address; c.Note = dto.Note; c.PortalEnabled = dto.PortalEnabled;
        c.PortalPhone = PhoneHelper.Normalize(dto.PortalPhone);
        if (!string.IsNullOrEmpty(dto.PortalPassword))
            c.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PortalPassword);
        await _db.SaveChangesAsync();
        return MapToDto(c);
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new Exception("Counterparty not found");
        c.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<CounterpartyBalanceDto> GetBalanceAsync(int tenantId, int counterpartyId)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == counterpartyId && x.TenantId == tenantId)
            ?? throw new Exception("Counterparty not found");
        var debt = await _db.Debts
            .Where(d => d.TenantId == tenantId && d.CounterpartyId == counterpartyId)
            .Select(d => d.Amount).FirstOrDefaultAsync();
        return new CounterpartyBalanceDto
        {
            CounterpartyId = c.Id, CounterpartyName = c.Name, DebtAmount = debt
        };
    }

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(int tenantId, int counterpartyId)
    {
        return await _db.PaymentHistories
            .Where(p => p.TenantId == tenantId && p.CounterpartyId == counterpartyId)
            .Include(p => p.Counterparty).Include(p => p.RecordedByUser)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentHistoryDto
            {
                Id = p.Id, CounterpartyId = p.CounterpartyId,
                CounterpartyName = p.Counterparty.Name, TransferId = p.TransferId,
                Amount = p.Amount, Method = p.Method, PaidAt = p.PaidAt,
                Note = p.Note, RecordedByUserName = p.RecordedByUser.FullName
            }).ToListAsync();
    }

    private static CounterpartyDto MapToDto(Counterparty c) => new()
    {
        Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
        Address = c.Address, Note = c.Note, PortalEnabled = c.PortalEnabled,
        PortalPhone = c.PortalPhone
    };
}
