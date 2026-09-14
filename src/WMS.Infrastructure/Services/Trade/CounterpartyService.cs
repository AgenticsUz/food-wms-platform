using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

// ⚠️ F6: global `Organization` katalogi (INN bo'yicha tenantlararo moslash, «platformada bor»
// belgisi) va kontragent portali o'chdi (D10, D8). INN kontragentning o'zida, faqat tekshiriladi.
public class CounterpartyService : ICounterpartyService
{
    /// <summary>Qidiruvda ko'pi bilan shuncha nomzod (ro'yxat sahifalanmaydi).</summary>
    private const int SearchLimit = 50;

    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    private readonly ISearchService _search;
    public CounterpartyService(WmsDbContext db, ITenantStateService tenantState, ISearchService search)
    { _db = db; _tenantState = tenantState; _search = search; }

    public async Task<List<CounterpartyDto>> GetAllAsync(CounterpartyType? type = null, string? search = null)
    {
        await EnsureCounterpartyTypeAllowedAsync(type);

        var q = _db.Counterparties.AsQueryable();
        if (type.HasValue) q = q.Where(c => c.Type == type.Value);

        // Qidiruv: nomzodlarni `ISearchService` beradi, tur filtri esa baribir SHU YERDA
        // qoladi — feature tekshiruvi bilan bitta joyda tursin.
        List<Guid> ranked = [];
        if (!string.IsNullOrWhiteSpace(search))
        {
            IReadOnlyList<CounterpartySearchCandidate> candidates =
                await _search.FindCounterpartiesAsync(search, SearchLimit);
            ranked = candidates.Select(c => c.Id).ToList();
            if (ranked.Count == 0) return [];

            q = q.Where(c => ranked.Contains(c.Id));
        }

        List<CounterpartyDto> rows = await q.OrderBy(c => c.Name).Select(c => new CounterpartyDto
        {
            Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
            Address = c.Address, Note = c.Note, AgentId = c.AgentId,
            AgentName = c.Agent != null ? c.Agent.Name : null,
            Inn = c.Inn
        }).ToListAsync();

        if (ranked.Count == 0) return rows;

        // `IN (...)` tartibni SAQLAMAYDI — o'xshashlik bali tartibini qaytarib qo'yamiz.
        Dictionary<Guid, int> rank = ranked.Index().ToDictionary(x => x.Item, x => x.Index);
        return rows.OrderBy(r => rank[r.Id]).ToList();
    }

    /// <summary>
    /// Suppliers and clients are sold separately (`counterparties.suppliers` /
    /// `counterparties.clients`), and the list endpoint is filtered by type, so the check
    /// belongs here rather than in an attribute. Asking for "both" needs either feature.
    /// </summary>
    private async Task EnsureCounterpartyTypeAllowedAsync(CounterpartyType? type)
    {
        if (_db.CurrentTenantId is not { } tenantId) return;

        var state = await _tenantState.GetAsync(tenantId);
        if (state == null) return;

        bool suppliers = state.EnabledFeatures.Contains(FeatureCodes.CounterpartiesSuppliers);
        bool clients = state.EnabledFeatures.Contains(FeatureCodes.CounterpartiesClients);

        switch (type)
        {
            case CounterpartyType.Supplier when !suppliers:
                throw new FeatureDisabledException(FeatureCodes.CounterpartiesSuppliers);
            case CounterpartyType.Client when !clients:
                throw new FeatureDisabledException(FeatureCodes.CounterpartiesClients);
            case null or CounterpartyType.Both when !suppliers && !clients:
                throw new FeatureDisabledException(FeatureCodes.CounterpartiesClients);
        }
    }

    public async Task<CounterpartyDto> GetByIdAsync(Guid id)
    {
        return await _db.Counterparties
            .Where(c => c.Id == id)
            .Select(c => new CounterpartyDto
            {
                Id = c.Id, Name = c.Name, Type = c.Type, Phone = c.Phone,
                Address = c.Address, Note = c.Note, AgentId = c.AgentId,
                AgentName = c.Agent != null ? c.Agent.Name : null,
                Inn = c.Inn
            }).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Counterparty not found");
    }

    public async Task<CounterpartyDto> CreateAsync(CreateCounterpartyDto dto)
    {
        await EnsureCounterpartyTypeAllowedAsync(dto.Type);
        await ValidateAgentAsync(dto.AgentId);

        var c = new Counterparty
        {
            // Qidiruv ustuni nom bilan BIRGA (P2.1) — sabab `SearchNormalizer` izohida.
            Name = dto.Name, NameSearch = SearchNormalizer.Normalize(dto.Name),
            Type = dto.Type, Phone = PhoneHelper.Normalize(dto.Phone),
            Inn = NormalizeInn(dto.Inn),
            Address = dto.Address, Note = dto.Note, AgentId = dto.AgentId
        };
        _db.Counterparties.Add(c);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(c.Id);
    }

    public async Task<CounterpartyDto> UpdateAsync(Guid id, UpdateCounterpartyDto dto)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Counterparty not found");
        await ValidateAgentAsync(dto.AgentId);

        // Bo'sh INN — bog'lanishni tozalaydi; noto'g'ri INN — xato (SQLite davridagi qoida).
        c.Inn = NormalizeInn(dto.Inn);
        c.Name = dto.Name; c.NameSearch = SearchNormalizer.Normalize(dto.Name);
        c.Type = dto.Type; c.Phone = PhoneHelper.Normalize(dto.Phone);
        c.Address = dto.Address; c.Note = dto.Note; c.AgentId = dto.AgentId;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(c.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var c = await _db.Counterparties.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Counterparty not found");
        c.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<CounterpartyBalanceDto> GetBalanceAsync(Guid counterpartyId)
    {
        var c = await _db.Counterparties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == counterpartyId)
            ?? throw new NotFoundException("Counterparty not found");
        var debt = await _db.Debts
            .Where(d => d.CounterpartyId == counterpartyId)
            .Select(d => d.Amount).FirstOrDefaultAsync();
        return new CounterpartyBalanceDto
        {
            CounterpartyId = c.Id, CounterpartyName = c.Name, DebtAmount = debt
        };
    }

    public async Task<List<PaymentHistoryDto>> GetPaymentsAsync(Guid counterpartyId)
    {
        return await _db.PaymentHistories
            .Where(p => p.CounterpartyId == counterpartyId)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentHistoryDto
            {
                Id = p.Id, CounterpartyId = p.CounterpartyId,
                CounterpartyName = p.Counterparty.Name, TransferId = p.TransferId,
                Amount = p.Amount, Method = p.Method, PaidAt = p.PaidAt,
                Note = p.Note, RecordedByUserName = p.RecordedByUser.FullName
            }).ToListAsync();
    }

    private async Task ValidateAgentAsync(Guid? agentId)
    {
        if (!agentId.HasValue) return;
        var agentExists = await _db.Agents.AnyAsync(a => a.Id == agentId.Value);
        if (!agentExists) throw new NotFoundException("Agent not found");
    }

    /// <summary>
    /// STIR: 9 ta raqam, bo'shliq va chiziqcha e'tiborsiz. Bo'sh — <see langword="null"/>;
    /// bor, lekin buzuq — xato.
    /// </summary>
    /// <remarks>SQLite davridagi <c>OrganizationMatcher.NormalizeInn</c> dan (katalog o'chdi, qoida qoldi).</remarks>
    private static string? NormalizeInn(string? inn)
    {
        if (string.IsNullOrWhiteSpace(inn)) return null;

        // ASCII raqam: `char.IsDigit` arabcha-hindcha raqamlarni ham o'tkazib yuborardi.
        var cleaned = new string(inn.Where(char.IsAsciiDigit).ToArray());
        if (cleaned.Length != 9)
            throw new AppException("INN (STIR) must be exactly 9 digits");
        return cleaned;
    }
}
