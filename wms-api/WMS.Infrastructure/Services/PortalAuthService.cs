using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WMS.Application.DTOs.Finance;
using WMS.Application.DTOs.Portal;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class PortalAuthService : IPortalAuthService
{
    private readonly WmsDbContext _db;
    private readonly IConfiguration _config;

    public PortalAuthService(WmsDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<PortalAuthResponseDto> LoginAsync(PortalLoginDto dto)
    {
        var counterparty = await _db.Counterparties
            .FirstOrDefaultAsync(c => c.PortalPhone == dto.Phone && c.PortalEnabled)
            ?? throw new Exception("Invalid credentials or portal not enabled");

        if (string.IsNullOrEmpty(counterparty.PortalPasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, counterparty.PortalPasswordHash))
            throw new Exception("Invalid credentials");

        var claims = new[]
        {
            new Claim("counterpartyId", counterparty.Id.ToString()),
            new Claim("tenantId", counterparty.TenantId.ToString()),
            new Claim("portalType", counterparty.Type.ToString()),
            new Claim("isPortal", "true")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new PortalAuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Counterparty = new PortalCounterpartyDto
            {
                Id = counterparty.Id, Name = counterparty.Name,
                Type = counterparty.Type, TenantId = counterparty.TenantId
            }
        };
    }

    public async Task<PortalCounterpartyDto> GetCurrentAsync(int counterpartyId)
    {
        var c = await _db.Counterparties.FindAsync(counterpartyId)
            ?? throw new Exception("Counterparty not found");
        return new PortalCounterpartyDto
        {
            Id = c.Id, Name = c.Name, Type = c.Type, TenantId = c.TenantId
        };
    }

    public async Task<List<TransferDto>> GetTransfersAsync(int counterpartyId, int tenantId, int page, int pageSize)
    {
        return await _db.Transfers
            .Where(t => t.TenantId == tenantId && t.CounterpartyId == counterpartyId)
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TransferDto
            {
                Id = t.Id, Type = t.Type, Status = t.Status,
                FromWarehouseId = t.FromWarehouseId, FromWarehouseName = t.FromWarehouse != null ? t.FromWarehouse.Name : null,
                ToWarehouseId = t.ToWarehouseId, ToWarehouseName = t.ToWarehouse != null ? t.ToWarehouse.Name : null,
                Note = t.Note, ConfirmedAt = t.ConfirmedAt, CreatedAt = t.CreatedAt,
                TotalAmount = t.Items.Sum(i => i.Quantity * i.UnitPrice),
                Items = t.Items.Select(i => new TransferItemDto
                {
                    Id = i.Id, ProductId = i.ProductId, ProductName = i.Product.Name,
                    UnitShortName = i.Product.Unit.ShortName,
                    Quantity = i.Quantity, UnitPrice = i.UnitPrice,
                    TotalPrice = i.Quantity * i.UnitPrice
                }).ToList()
            }).ToListAsync();
    }

    public async Task<TransferDto> GetTransferByIdAsync(int counterpartyId, int tenantId, int transferId)
    {
        var t = await _db.Transfers
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.TenantId == tenantId && t.CounterpartyId == counterpartyId)
            ?? throw new Exception("Transfer not found");

        return new TransferDto
        {
            Id = t.Id, Type = t.Type, Status = t.Status,
            FromWarehouseId = t.FromWarehouseId, FromWarehouseName = t.FromWarehouse?.Name,
            ToWarehouseId = t.ToWarehouseId, ToWarehouseName = t.ToWarehouse?.Name,
            Note = t.Note, ConfirmedAt = t.ConfirmedAt, CreatedAt = t.CreatedAt,
            TotalAmount = t.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = t.Items.Select(i => new TransferItemDto
            {
                Id = i.Id, ProductId = i.ProductId, ProductName = i.Product.Name,
                UnitShortName = i.Product.Unit.ShortName,
                Quantity = i.Quantity, UnitPrice = i.UnitPrice,
                TotalPrice = i.Quantity * i.UnitPrice
            }).ToList()
        };
    }

    public async Task<PortalFinanceDto> GetFinanceAsync(int counterpartyId, int tenantId)
    {
        var debt = await _db.Debts
            .Where(d => d.TenantId == tenantId && d.CounterpartyId == counterpartyId)
            .Select(d => d.Amount).FirstOrDefaultAsync();
        return new PortalFinanceDto { Balance = -debt, TotalDebt = debt };
    }

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(int counterpartyId, int tenantId)
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
}
