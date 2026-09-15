using Microsoft.EntityFrameworkCore;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai;

/// <summary>
/// Metering: har chaqiriq hisobda (F10 §0.8) va uchta darvoza — kalit, feature, kvota.
/// </summary>
/// <remarks>
/// <para>
/// Nega haqiqiy baza: sarf <c>INSERT … ON CONFLICT DO UPDATE</c> bilan yoziladi va uning
/// arbitri — QISMAN noyob indeks. InMemory provayderida na indeks, na <c>ON CONFLICT</c>
/// bor, ya'ni eng muhim da'vo — «ikkinchi so'rov qatorni OSHIRADI, ustiga yozmaydi» —
/// umuman o'lchanmasdi.
/// </para>
/// <para>
/// ⚠️ <c>ai_daily_cost</c> — PLATFORMA jadvali: undagi qator hamma testga umumiy. Kunlik
/// shift testi shuning uchun avvalgi qiymatni eslab qoladi va o'zidan keyin qaytaradi;
/// aks holda keyingi testlar «AI vaqtincha ishlamayapti» deb yiqilardi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class AiMeteringTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public AiMeteringTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Har_sorov_ai_usage_ga_yoziladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        IAiMetering metering = scope.Service<IAiMetering>();

        // 1 mln kirish tokeni = 2.00 USD (fixture'dagi narx jadvali).
        await metering.RecordAsync("claude-sonnet-5", new LlmUsage(1_000_000, 0, 0, 0), TestContext.Current.CancellationToken);
        await metering.RecordAsync("claude-sonnet-5", new LlmUsage(500_000, 100_000, 0, 0), TestContext.Current.CancellationToken);

        AiUsage row = await scope.Db.AiUsages.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        // Ikkinchi so'rov qatorni OSHIRDI — ustiga yozmadi.
        row.Requests.ShouldBe(2);
        row.InputTokens.ShouldBe(1_500_000);
        row.OutputTokens.ShouldBe(100_000);

        // 2.00 + (1.00 + 1.00) = 4.00
        row.UsdCost.ShouldBe(4.0m);
        row.Day.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task Narx_jadvalida_yoq_model_ham_yoziladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        await scope.Service<IAiMetering>()
            .RecordAsync("model-yoq", new LlmUsage(1_000, 500, 0, 0), TestContext.Current.CancellationToken);

        AiUsage row = await scope.Db.AiUsages.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        // Token soni — haqiqat, u yo'qolmaydi; dollar esa 0 (logda ogohlantirish bilan).
        row.InputTokens.ShouldBe(1_000);
        row.UsdCost.ShouldBe(0m);
    }

    [Fact]
    public async Task Kalit_yoq_bolsa_ai_ochiq()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        await EnableAiAsync(scope, tenant);

        // `Ai__ApiKey` bo'sh — modul o'chiq, lekin bu NOSOZLIK emas: 403 + `ai_disabled`.
        scope.Service<FakeLlmClient>().IsConfigured = false;

        AiException error = await Should.ThrowAsync<AiException>(
            () => scope.Service<IAiMetering>().EnsureAvailableAsync(TestContext.Current.CancellationToken));

        error.Code.ShouldBe(AiErrorCodes.Disabled);
    }

    [Fact]
    public async Task Feature_yoqilmagan_tenantda_ai_ochiq()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        // `ai.chat` hech bir planga kirmaydi va sukuti o'chiq — override yo'q, demak o'chiq.
        AiException error = await Should.ThrowAsync<AiException>(
            () => scope.Service<IAiMetering>().EnsureAvailableAsync(TestContext.Current.CancellationToken));

        error.Code.ShouldBe(AiErrorCodes.Disabled);
    }

    [Fact]
    public async Task Feature_yoqilgan_tenantda_ai_ochiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        await EnableAiAsync(scope, tenant);

        await Should.NotThrowAsync(
            () => scope.Service<IAiMetering>().EnsureAvailableAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Kvota_tugasa_rad_etiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        await EnableAiAsync(scope, tenant);
        await AttachPlanAsync(scope, tenant, monthlyAiRequests: 1);

        IAiMetering metering = scope.Service<IAiMetering>();

        // Kvota 1 ta: birinchi so'rov o'tadi.
        await metering.EnsureAvailableAsync(TestContext.Current.CancellationToken);
        await metering.RecordAsync("claude-sonnet-5", new LlmUsage(10, 10, 0, 0), TestContext.Current.CancellationToken);

        AiException error = await Should.ThrowAsync<AiException>(
            () => metering.EnsureAvailableAsync(TestContext.Current.CancellationToken));

        // 402: «planni ko'taring» oqimi, 403 emas — aybdor sozlama emas, hajm.
        error.Code.ShouldBe(AiErrorCodes.QuotaExceeded);
        error.Status.ShouldBe(402);
    }

    [Fact]
    public async Task Kunlik_shift_oshsa_vaqtincha_ishlamaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        ResetLlm(scope);

        await EnableAiAsync(scope, tenant);

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        AiDailyCost? row = await scope.Db.AiDailyCosts.FirstOrDefaultAsync(c => c.Day == today, TestContext.Current.CancellationToken);
        decimal restore = row?.UsdCost ?? 0m;

        if (row is null)
        {
            row = new AiDailyCost { Day = today, Requests = 1 };
            scope.Db.AiDailyCosts.Add(row);
        }

        // Sukut shift 10 USD (`AiOptions.DailyUsdCap`).
        row.UsdCost = 999m;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        try
        {
            AiException error = await Should.ThrowAsync<AiException>(
                () => scope.Service<IAiMetering>().EnsureAvailableAsync(TestContext.Current.CancellationToken));

            // 503: aybdor tenant emas, shuning uchun «keyinroq urinib ko'ring».
            error.Code.ShouldBe(AiErrorCodes.Unavailable);
            error.Status.ShouldBe(503);
        }
        finally
        {
            // Platforma jadvali — qiymat qaytarilmasa keyingi testlar ham yiqilardi.
            row.UsdCost = restore;
            await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Fake klient — singleton: har test uni o'zi uchun tozalaydi.</summary>
    private static void ResetLlm(WmsTenantScope scope) => scope.Service<FakeLlmClient>().Reset();

    /// <summary>Console operatori qiladigan narsa: tenantga <c>ai.chat</c> override'i.</summary>
    private static async Task EnableAiAsync(WmsTenantScope scope, TestTenant tenant)
    {
        scope.Db.TenantFeatures.Add(new TenantFeature { FeatureCode = FeatureCodes.AiChat, IsEnabled = true });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Holat keshdan kelsa o'zgarish ko'rinmasdi (Console ham shuni chaqiradi).
        scope.Service<ITenantStateService>().Invalidate(tenant.Id);
    }

    /// <summary>Tenantga AI kvotasi bor plan biriktiradi.</summary>
    /// <remarks>
    /// Fixture tenantni ATAYLAB plansiz yaratadi (limitlar cheksiz bo'lsin) — kvota testi
    /// uchun plan aynan shu yerda, faqat shu tenantga qo'yiladi.
    /// </remarks>
    private static async Task AttachPlanAsync(WmsTenantScope scope, TestTenant tenant, int monthlyAiRequests)
    {
        Plan plan = new()
        {
            Name = $"ai-{monthlyAiRequests}",
            Code = $"ai-{Guid.NewGuid().ToString("N")[..8]}",
            FeatureCodes = FeatureCodes.AiChat,
            MaxAiRequestsPerMonth = monthlyAiRequests,
        };

        scope.Db.Plans.Add(plan);

        Tenant row = await scope.Db.Tenants.SingleAsync(t => t.Id == tenant.Id, TestContext.Current.CancellationToken);
        row.PlanId = plan.Id;

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        scope.Service<ITenantStateService>().Invalidate(tenant.Id);
    }
}
