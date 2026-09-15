using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Ai;

/// <inheritdoc />
public sealed class AiMetering : IAiMetering
{
    /// <summary>
    /// Tenantning kunlik satri: bor bo'lsa oshiriladi, yo'q bo'lsa yaratiladi.
    /// </summary>
    /// <remarks>
    /// ⚠️ O'qib-o'zgartirib-yozish EMAS: bir tenantdan parallel kelgan ikki so'rov bir xil
    /// kunga yozadi va oxirgisi birinchisining tokenini yo'q qilardi. <c>ON CONFLICT DO
    /// UPDATE … + EXCLUDED</c> — yig'indini BAZA qo'shadi (<c>DocumentNumbers</c> naqshi).
    /// Arbitr — qisman noyob indeks, shuning uchun predikat gapda AYNAN takrorlanadi.
    /// </remarks>
    private const string UsageSql = """
        INSERT INTO wms.ai_usage (
            id, tenant_id, day, model, requests,
            input_tokens, output_tokens, cache_write_tokens, cache_read_tokens,
            usd_cost, created_at, updated_at, is_deleted)
        VALUES (
            @id, NULLIF(current_setting('app.tenant_id', true), '')::uuid, @day, @model, 1,
            @input, @output, @cacheWrite, @cacheRead,
            @cost, now(), now(), false)
        ON CONFLICT (tenant_id, day, model) WHERE is_deleted = false
        DO UPDATE SET
            requests = wms.ai_usage.requests + 1,
            input_tokens = wms.ai_usage.input_tokens + EXCLUDED.input_tokens,
            output_tokens = wms.ai_usage.output_tokens + EXCLUDED.output_tokens,
            cache_write_tokens = wms.ai_usage.cache_write_tokens + EXCLUDED.cache_write_tokens,
            cache_read_tokens = wms.ai_usage.cache_read_tokens + EXCLUDED.cache_read_tokens,
            usd_cost = wms.ai_usage.usd_cost + EXCLUDED.usd_cost,
            updated_at = now();
        """;

    /// <summary>Platformaning kunlik satri — tenant ustuni yo'q (sababi <see cref="AiDailyCost"/> da).</summary>
    private const string DailyCostSql = """
        INSERT INTO wms.ai_daily_cost (id, day, requests, usd_cost, created_at, updated_at, is_deleted)
        VALUES (@id, @day, 1, @cost, now(), now(), false)
        ON CONFLICT (day) WHERE is_deleted = false
        DO UPDATE SET
            requests = wms.ai_daily_cost.requests + 1,
            usd_cost = wms.ai_daily_cost.usd_cost + EXCLUDED.usd_cost,
            updated_at = now();
        """;

    private readonly WmsDbContext _db;
    private readonly ILlmClient _llm;
    private readonly ITenantStateService _tenantState;
    private readonly AiOptions _options;
    private readonly ILogger<AiMetering> _logger;

    public AiMetering(
        WmsDbContext db,
        ILlmClient llm,
        ITenantStateService tenantState,
        IOptions<AiOptions> options,
        ILogger<AiMetering> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _db = db;
        _llm = llm;
        _tenantState = tenantState;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnsureAvailableAsync(CancellationToken cancellationToken = default)
    {
        // 1. Kalit. Bo'sh — modul o'chiq va bu XATO EMAS: qolgan tizim ishlayveradi.
        if (!_llm.IsConfigured)
        {
            throw AiException.Disabled();
        }

        // 2. Tenant konteksti. Yo'q bo'lsa kimning hisobiga yozishni bilmaymiz — fail-closed.
        if (_db.CurrentTenantId is not { } tenantId)
        {
            throw AiException.Disabled();
        }

        // 3. Feature — Console'dagi to'liq o'chirgich.
        TenantState? state = await _tenantState.GetAsync(tenantId, cancellationToken);
        if (state is null || !state.EnabledFeatures.Contains(FeatureCodes.AiChat))
        {
            throw AiException.Disabled();
        }

        // 4. Platforma shifti — tenant kvotasidan OLDIN: hisob portlayotganda hamma uchun
        // to'xtaydi va bu «vaqtincha ishlamayapti», «sizning kvotangiz tugadi» emas.
        await EnsureDailyCapAsync(cancellationToken);

        // 5. Tenantning oylik kvotasi.
        await EnsureMonthlyQuotaAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordAsync(string model, LlmUsage usage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usage);

        // Model nomi javobdan keladi (provayder alias'ni yechib berishi mumkin); bo'sh
        // bo'lsa konfigdagi nom yoziladi — «qaysi model» ustuni hech qachon bo'sh qolmasin.
        string effective = string.IsNullOrWhiteSpace(model) ? _llm.Model : model;

        if (!AiPricing.TryCost(_options, effective, usage, out decimal cost))
        {
            // Sarf BARIBIR yoziladi (token soni — haqiqat), lekin dollar 0 bo'lib qoladi.
            // Shuning uchun bu jim o'tmaydi: kunlik shift ham o'sha nolga qarab yashab ketardi.
            _logger.LogWarning(
                "'{Model}' uchun narx jadvali yo'q (Ai:Pricing) — xarajat 0 deb yozildi, kunlik shift uni ko'rmaydi.",
                effective);
        }

        DateOnly day = DateOnly.FromDateTime(DateTime.UtcNow);

        await _db.Database.ExecuteSqlRawAsync(
            UsageSql,
            [
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("day", day),
                new NpgsqlParameter("model", effective),
                new NpgsqlParameter("input", usage.InputTokens),
                new NpgsqlParameter("output", usage.OutputTokens),
                new NpgsqlParameter("cacheWrite", usage.CacheWriteTokens),
                new NpgsqlParameter("cacheRead", usage.CacheReadTokens),
                new NpgsqlParameter("cost", cost),
            ],
            cancellationToken);

        await _db.Database.ExecuteSqlRawAsync(
            DailyCostSql,
            [
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("day", day),
                new NpgsqlParameter("cost", cost),
            ],
            cancellationToken);
    }

    /// <summary>
    /// Platformaning kunlik dollar shifti.
    /// </summary>
    /// <remarks>
    /// Bu — ilova darajasidagi to'r, birinchi himoya emas: undan tashqarida Anthropic
    /// Console'ning kalit bo'yicha sarf limiti turishi kerak. Shift oshganda javob 503
    /// («keyinroq urinib ko'ring»), 402 emas — aybdor tenant emas.
    /// </remarks>
    private async Task EnsureDailyCapAsync(CancellationToken cancellationToken)
    {
        if (_options.DailyUsdCap <= 0m)
        {
            return;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        decimal spent = await _db.AiDailyCosts.AsNoTracking()
            .Where(c => c.Day == today)
            .Select(c => c.UsdCost)
            .FirstOrDefaultAsync(cancellationToken);

        if (spent >= _options.DailyUsdCap)
        {
            _logger.LogError(
                "AI kunlik shifti oshdi: {Spent} USD >= {Cap} USD — hamma tenant uchun to'xtatildi.",
                spent,
                _options.DailyUsdCap);
            throw AiException.Unavailable();
        }
    }

    /// <summary>Tenantning shu OYDAGI so'rovlari plan kvotasidan oshmasin.</summary>
    /// <remarks>
    /// Kvota 0 yoki plan yo'q — cheklanmagan (qolgan plan limitlari bilan bir xil qoida).
    /// Hisob <c>ai_usage</c> dan: u RLS ostida, ya'ni yig'indi o'z-o'zidan joriy tenantniki.
    /// </remarks>
    private async Task EnsureMonthlyQuotaAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        int limit = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId && t.PlanId != null)
            .Select(t => t.Plan!.MaxAiRequestsPerMonth)
            .FirstOrDefaultAsync(cancellationToken);

        if (limit <= 0)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateOnly monthStart = new(now.Year, now.Month, 1);

        int used = await _db.AiUsages.AsNoTracking()
            .Where(u => u.Day >= monthStart)
            .SumAsync(u => u.Requests, cancellationToken);

        if (used >= limit)
        {
            throw AiException.QuotaExceeded(limit);
        }
    }
}
