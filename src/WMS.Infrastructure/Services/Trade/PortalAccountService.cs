using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Portal;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

/// <summary>
/// Kabinet hisobini kontragent/agent kartasiga biriktirish (tenant adminining yuzasi).
/// </summary>
/// <remarks>
/// <para>
/// Hisobning O'ZI Identity'da ochiladi — SPA <c>/tenant/v1/members</c> ga <c>role: client</c>
/// (yoki <c>agent</c>) bilan boradi va BIR MARTALIK vaqtinchalik parolni ko'rsatadi. Bu yerda
/// faqat bog'lanish yoziladi: parol, bloklash va telefon WMS'ga hech qachon kelmaydi (P4).
/// </para>
/// <para>
/// ⚠️ Bitta <c>sub</c> tenantda bitta kartaga: noyob indeks buni bazada ham ushlaydi, lekin
/// xato tushunarli bo'lishi uchun avval o'qib ko'riladi.
/// </para>
/// </remarks>
public sealed class PortalAccountService : IPortalAccountService
{
    private readonly WmsDbContext _db;

    public PortalAccountService(WmsDbContext db) => _db = db;

    public async Task<PortalAccountDto> GetCounterpartyAccountAsync(Guid counterpartyId, CancellationToken cancellationToken = default)
        => Describe((await FindCounterpartyAsync(counterpartyId, cancellationToken)).IdentitySub);

    public async Task<PortalAccountDto> LinkCounterpartyAsync(Guid counterpartyId, Guid identitySub, CancellationToken cancellationToken = default)
    {
        Counterparty counterparty = await FindCounterpartyAsync(counterpartyId, cancellationToken);
        await EnsureFreeAsync(identitySub, counterpartyId, null, cancellationToken);

        counterparty.IdentitySub = identitySub;
        await _db.SaveChangesAsync(cancellationToken);
        return Describe(counterparty.IdentitySub);
    }

    public async Task<PortalAccountDto> UnlinkCounterpartyAsync(Guid counterpartyId, CancellationToken cancellationToken = default)
    {
        Counterparty counterparty = await FindCounterpartyAsync(counterpartyId, cancellationToken);

        // Identity'dagi hisob O'CHIRILMAYDI — uni «Kirish hisoblari» boshqaradi. Bu yerda
        // faqat bog'lanish uziladi: keyingi so'rovdan kabinet 403 beradi.
        counterparty.IdentitySub = null;
        await _db.SaveChangesAsync(cancellationToken);
        return Describe(null);
    }

    public async Task<PortalAccountDto> GetAgentAccountAsync(Guid agentId, CancellationToken cancellationToken = default)
        => Describe((await FindAgentAsync(agentId, cancellationToken)).IdentitySub);

    public async Task<PortalAccountDto> LinkAgentAsync(Guid agentId, Guid identitySub, CancellationToken cancellationToken = default)
    {
        Agent agent = await FindAgentAsync(agentId, cancellationToken);
        await EnsureFreeAsync(identitySub, null, agentId, cancellationToken);

        agent.IdentitySub = identitySub;
        await _db.SaveChangesAsync(cancellationToken);
        return Describe(agent.IdentitySub);
    }

    public async Task<PortalAccountDto> UnlinkAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        Agent agent = await FindAgentAsync(agentId, cancellationToken);
        agent.IdentitySub = null;
        await _db.SaveChangesAsync(cancellationToken);
        return Describe(null);
    }

    private async Task<Counterparty> FindCounterpartyAsync(Guid id, CancellationToken cancellationToken)
        => await _db.Counterparties.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
           ?? throw new NotFoundException("Counterparty not found");

    private async Task<Agent> FindAgentAsync(Guid id, CancellationToken cancellationToken)
        => await _db.Agents.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
           ?? throw new NotFoundException("Agent not found");

    /// <summary>Shu <c>sub</c> boshqa kartaga biriktirilgan bo'lsa — aniq xato.</summary>
    private async Task EnsureFreeAsync(Guid identitySub, Guid? exceptCounterpartyId, Guid? exceptAgentId, CancellationToken cancellationToken)
    {
        if (identitySub == Guid.Empty)
        {
            throw new AppException("Portal account is required");
        }

        string? taken = await _db.Counterparties.AsNoTracking()
            .Where(c => c.IdentitySub == identitySub && c.Id != exceptCounterpartyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        taken ??= await _db.Agents.AsNoTracking()
            .Where(a => a.IdentitySub == identitySub && a.Id != exceptAgentId)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (taken is not null)
        {
            throw new AppException("This login account is already linked to {0}", taken);
        }
    }

    private static PortalAccountDto Describe(Guid? sub) =>
        new() { Enabled = sub.HasValue, IdentitySub = sub };
}
