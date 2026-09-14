using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.DTOs.Portal;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

/// <summary>
/// Kabinet yuzasi — kontragent va agentning O'Z oldi-berdisi (F9; D8 ning ikkinchi bosqichi).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Ikki qavatli izolyatsiya: tenant — RLS va global filtr bilan (boshqa servislardagidek),
/// KONTRAGENT esa shu yerdagi filtr bilan. Aktyor HAR DOIM tokendagi <c>sub</c> dan yechiladi
/// (<see cref="ResolveAsync"/>), chaqiruvchidan olinmaydi.
/// </para>
/// <para>
/// ⚠️ Nega kabinet ilovaning o'z endpoint'laridan foydalanmaydi: ular ruxsat kodiga tayanadi va
/// bir kontragentga cheklanmagan. Kabinet roli (`client`/`agent`) ruxsatsiz — ya'ni ilovaning
/// hamma endpoint'i ular uchun avtomatik 403, bu yuza esa ATAYLAB ochilgan yagona eshik.
/// </para>
/// </remarks>
public sealed class PortalService : IPortalService
{
    private readonly WmsDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;
    private readonly ITenantStateService _tenantState;

    public PortalService(WmsDbContext db, ICurrentUser user, ICurrentTenant tenant, ITenantStateService tenantState)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
        _tenantState = tenantState;
    }

    /// <summary>Kabinet egasi: kontragent yoki agent.</summary>
    private sealed record PortalActor(PortalActorKind Kind, Guid Id);

    /// <summary>
    /// Tokendagi <c>sub</c> bo'yicha kabinet egasini topadi; topilmasa 403.
    /// </summary>
    /// <remarks>
    /// Kontragent avval qaraladi: bir odam ham mijoz, ham agent bo'lishi nazariy jihatdan
    /// mumkin, lekin kabinet bitta bo'lishi kerak va oldi-berdi ko'rinishi muhimroq.
    /// </remarks>
    private async Task<PortalActor> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_user.Sub is not { } sub || sub == Guid.Empty)
        {
            throw new ForbiddenException("This account has no portal access in this organization");
        }

        Guid counterpartyId = await _db.Counterparties.AsNoTracking()
            .Where(c => c.IdentitySub == sub)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (counterpartyId != Guid.Empty)
        {
            return new PortalActor(PortalActorKind.Counterparty, counterpartyId);
        }

        Guid agentId = await _db.Agents.AsNoTracking()
            .Where(a => a.IdentitySub == sub && a.IsActive)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (agentId != Guid.Empty)
        {
            return new PortalActor(PortalActorKind.Agent, agentId);
        }

        throw new ForbiddenException("This account has no portal access in this organization");
    }

    private async Task<Guid> ResolveAgentAsync(CancellationToken cancellationToken)
    {
        PortalActor actor = await ResolveAsync(cancellationToken);
        return actor.Kind == PortalActorKind.Agent
            ? actor.Id
            : throw new ForbiddenException("This section is for sales agents only");
    }

    public async Task<PortalMeDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        PortalActor actor = await ResolveAsync(cancellationToken);
        string tenantName = await TenantNameAsync(cancellationToken);

        if (actor.Kind == PortalActorKind.Agent)
        {
            Agent agent = await _db.Agents.AsNoTracking().FirstAsync(a => a.Id == actor.Id, cancellationToken);
            return new PortalMeDto
            {
                Id = agent.Id,
                Kind = PortalActorKind.Agent,
                Name = agent.Name,
                Phone = agent.Phone,
                TenantName = tenantName,
            };
        }

        Counterparty counterparty = await _db.Counterparties.AsNoTracking()
            .FirstAsync(c => c.Id == actor.Id, cancellationToken);

        return new PortalMeDto
        {
            Id = counterparty.Id,
            Kind = PortalActorKind.Counterparty,
            Name = counterparty.Name,
            CounterpartyType = counterparty.Type,
            Phone = counterparty.Phone,
            Inn = counterparty.Inn,
            TenantName = tenantName,
        };
    }

    public async Task<PortalFinanceDto> GetFinanceAsync(CancellationToken cancellationToken = default)
    {
        PortalActor actor = await ResolveAsync(cancellationToken);
        if (actor.Kind != PortalActorKind.Counterparty)
        {
            throw new ForbiddenException("This section is for clients and suppliers only");
        }

        decimal debt = await _db.Debts.AsNoTracking()
            .Where(d => d.CounterpartyId == actor.Id)
            .Select(d => d.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        // Aylanma — TASDIQLANGAN hujjatlar bo'yicha: kutilayotgan hujjat hali oldi-berdi emas.
        decimal turnover = await _db.Transfers.AsNoTracking()
            .Where(t => t.CounterpartyId == actor.Id && t.Status == TransferStatus.Confirmed)
            .SelectMany(t => t.Items)
            .SumAsync(i => (decimal?)(i.Quantity * i.UnitPrice), cancellationToken) ?? 0m;

        var payments = await _db.PaymentHistories.AsNoTracking()
            .Where(p => p.CounterpartyId == actor.Id)
            .GroupBy(p => 1)
            .Select(g => new { Total = g.Sum(p => p.Amount), Last = g.Max(p => (DateTime?)p.PaidAt) })
            .FirstOrDefaultAsync(cancellationToken);

        return new PortalFinanceDto
        {
            DebtAmount = debt,
            TotalTurnover = turnover,
            TotalPaid = payments?.Total ?? 0m,
            LastPaymentAt = payments?.Last,
        };
    }

    public async Task<List<PortalTransferDto>> GetTransfersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        PortalActor actor = await ResolveAsync(cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        return await TransfersFor(actor)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<PortalTransferDto> GetTransferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        PortalActor actor = await ResolveAsync(cancellationToken);

        // Begona hujjat «topilmadi» bo'ladi, «ruxsat yo'q» emas: id bo'yicha boshqa
        // kontragentning hujjati BOR-YO'Qligini bilib olish ham ortiqcha ma'lumot.
        return await TransfersFor(actor).Where(t => t.Id == id).Select(ToDto)
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new NotFoundException("Transfer not found");
    }

    public async Task<List<PortalPaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        PortalActor actor = await ResolveAsync(cancellationToken);
        if (actor.Kind != PortalActorKind.Counterparty)
        {
            throw new ForbiddenException("This section is for clients and suppliers only");
        }

        return await _db.PaymentHistories.AsNoTracking()
            .Where(p => p.CounterpartyId == actor.Id)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PortalPaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Method = p.Method,
                PaidAt = p.PaidAt,
                Note = p.Note,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PortalAgentSummaryDto> GetAgentSummaryAsync(CancellationToken cancellationToken = default)
    {
        Guid agentId = await ResolveAgentAsync(cancellationToken);

        Agent agent = await _db.Agents.AsNoTracking().FirstAsync(a => a.Id == agentId, cancellationToken);
        int clients = await _db.Counterparties.AsNoTracking().CountAsync(c => c.AgentId == agentId, cancellationToken);

        // Komissiya yozuvi tasdiqda yaratiladi va summasi o'sha paytdagi nusxa — hisobot
        // keyingi narx o'zgarishidan mustaqil.
        var commission = await _db.CommissionRecords.AsNoTracking()
            .Where(c => c.AgentId == agentId)
            .GroupBy(c => 1)
            .Select(g => new
            {
                Sales = g.Sum(c => c.SaleAmount),
                Earned = g.Sum(c => c.CommissionAmount),
                Paid = g.Sum(c => c.IsPaid ? c.CommissionAmount : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        decimal earned = commission?.Earned ?? 0m;
        decimal paid = commission?.Paid ?? 0m;

        return new PortalAgentSummaryDto
        {
            CommissionPercent = agent.CommissionPercent,
            ClientCount = clients,
            TotalSales = commission?.Sales ?? 0m,
            CommissionEarned = earned,
            CommissionPaid = paid,
            CommissionPending = earned - paid,
        };
    }

    public async Task<List<PortalAgentClientDto>> GetAgentClientsAsync(CancellationToken cancellationToken = default)
    {
        Guid agentId = await ResolveAgentAsync(cancellationToken);

        return await _db.Counterparties.AsNoTracking()
            .Where(c => c.AgentId == agentId)
            .OrderBy(c => c.Name)
            .Select(c => new PortalAgentClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                DebtAmount = _db.Debts.Where(d => d.CounterpartyId == c.Id).Select(d => d.Amount).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Aktyorga TEGISHLI hujjatlar. Kontragent — o'zining hujjatlari; agent — o'zi orqali
    /// o'tgan sotuvlar (<c>transfer.agent_id</c>), mijozlarining HAMMA hujjatlari emas:
    /// mijoz boshqa agent orqali ham olishi mumkin va u boshqaning ishi.
    /// </summary>
    private IQueryable<Transfer> TransfersFor(PortalActor actor) => _db.Transfers.AsNoTracking()
        .Where(t => actor.Kind == PortalActorKind.Counterparty
            ? t.CounterpartyId == actor.Id
            : t.AgentId == actor.Id);

    /// <remarks>Proyeksiya — ifoda daraxti: SQL'ga tushadi va ortiqcha ustun tanlanmaydi.</remarks>
    private static System.Linq.Expressions.Expression<Func<Transfer, PortalTransferDto>> ToDto =>
        t => new PortalTransferDto
        {
            Id = t.Id,
            Type = t.Type,
            Status = t.Status,
            TotalAmount = t.Items.Sum(i => i.Quantity * i.UnitPrice),
            Note = t.Note,
            CreatedAt = t.CreatedAt,
            ConfirmedAt = t.ConfirmedAt,
            Items = t.Items.Select(i => new PortalTransferItemDto
            {
                Id = i.Id,
                ProductName = i.Product.Name,
                UnitShortName = i.Product.Unit.ShortName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.Quantity * i.UnitPrice,
            }).ToList(),
        };

    private async Task<string> TenantNameAsync(CancellationToken cancellationToken)
    {
        if (_tenant.TenantId is not { } tenantId)
        {
            return string.Empty;
        }

        WMS.Application.Common.TenantState? state = await _tenantState.GetAsync(tenantId, cancellationToken);
        return state?.Name ?? string.Empty;
    }
}
