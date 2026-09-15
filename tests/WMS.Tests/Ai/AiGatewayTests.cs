using Microsoft.EntityFrameworkCore;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai;

/// <summary>
/// Gateway sikli: tool'lar, ruxsat chegarasi, suhbat oynasi va hisob.
/// </summary>
/// <remarks>
/// <para>
/// Model javoblari fixture'dan (<see cref="FakeLlmClient"/>) — tarmoqqa chiqilmaydi
/// (F10 §0.10). Shu sababli bu yerda MODEL emas, OQIM o'lchanadi: ruxsati yo'q tool
/// modelga berilmaydimi, tool natijasi keyingi so'rovga qo'shiladimi, sikl to'xtaydimi.
/// </para>
/// <para>
/// Modelning o'zini baholash — sinov to'plamining (eval) ishi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class AiGatewayTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public AiGatewayTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Toolsiz_javob_togridan_togri_qaytadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        AiTestSupport.ResetLlm(scope).EnqueueText("Salom! Nima bilan yordam bera olaman?");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(null, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Salom"),
            TestContext.Current.CancellationToken);

        answer.Text.ShouldContain("yordam");
        answer.Tools.ShouldBeEmpty();
        answer.ConversationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Tool_bajariladi_va_natijasi_modelga_qaytadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Sklad1");
        Product product = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");
        await TestData.AddStockAsync(scope.Db, warehouse, product, 34);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("stock_query", """{"productName":"Snikers"}"""));
        llm.EnqueueText("Sklad1 da 34 dona Snikers bor.");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(null, WmsPermissions.WarehouseView, WmsPermissions.ProductsView),
            new AiAskRequest(AiChannel.Web, "Snikers qancha qoldi?"),
            TestContext.Current.CancellationToken);

        // Tool bajarildi va natijasi yuzaga ham chiqdi.
        AiToolOutput output = answer.Tools.ShouldHaveSingleItem();
        output.Code.ShouldBe("stock_query");
        output.Text.ShouldContain("34");

        // ⚠️ Eng muhimi: IKKINCHI so'rovda tool natijasi bor. Bo'lmasa model o'z
        // chaqirig'ining javobini ko'rmasdi va savolni qaytadan so'rardi.
        llm.Requests.Count.ShouldBe(2);
        llm.Requests[1].Messages.Last().ToolResults.ShouldNotBeEmpty();
        llm.Requests[1].Messages.Last().ToolResults[0].Content.ShouldContain("34");
    }

    [Fact]
    public async Task Ruxsati_yoq_tool_modelga_BERILMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.EnqueueText("Bunga ruxsatingiz yo'q.");

        await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(null, WmsPermissions.WarehouseView),
            new AiAskRequest(AiChannel.Web, "Kim qancha qarzdor?"),
            TestContext.Current.CancellationToken);

        IReadOnlyList<string> offered = [.. llm.Requests[0].Tools.Select(t => t.Name)];

        // Ombor ruxsati bor — qoldiq ko'rinadi; moliya ruxsati yo'q — qarz KO'RINMAYDI.
        offered.ShouldContain("stock_query");
        offered.ShouldNotContain("debt_query");
        offered.ShouldNotContain("payment_history");
        offered.ShouldNotContain("finance_summary");
    }

    [Fact]
    public async Task Ruxsati_yoq_tool_zorlab_chaqirilsa_rad_etiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);

        // Model ro'yxatda KO'RMAGAN tool'ni nomi bilan chaqirdi (to'qib topdi).
        llm.Enqueue(AiTestSupport.ToolCall("debt_query"));
        llm.EnqueueText("Bu ma'lumotni ololmadim.");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(null, WmsPermissions.WarehouseView),
            new AiAskRequest(AiChannel.Web, "Qarzlarni ko'rsat"),
            TestContext.Current.CancellationToken);

        // Suhbat YIQILMAYDI: rad javobi tool natijasi bo'lib modelga qaytadi.
        answer.Tools.ShouldHaveSingleItem().Text.ShouldContain(AiErrorCodes.ToolForbidden);
        llm.Requests[1].Messages.Last().ToolResults[0].IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Sikl_cheksiz_aylanmaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);

        // Model hech qachon to'xtamaydi — bitta savol butun byudjetni yeb qo'ymasin.
        for (int i = 0; i < 10; i++)
        {
            llm.Enqueue(AiTestSupport.ToolCall("today_summary"));
        }

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(null, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Holat?"),
            TestContext.Current.CancellationToken);

        llm.Requests.Count.ShouldBe(6);
        answer.Text.ShouldContain("aniqroq");
    }

    [Fact]
    public async Task Suhbat_davom_etadi_va_tarix_modelga_ketadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.EnqueueText("Birinchi javob.").EnqueueText("Ikkinchi javob.");

        IAiGateway gateway = scope.Service<IAiGateway>();
        AiUser user = AiTestSupport.User(null, WmsPermissions.DashboardView);

        AiAnswer first = await gateway.AskAsync(
            user, new AiAskRequest(AiChannel.Web, "Birinchi savol"), TestContext.Current.CancellationToken);

        AiAnswer second = await gateway.AskAsync(
            user,
            new AiAskRequest(AiChannel.Web, "Ikkinchi savol", first.ConversationId),
            TestContext.Current.CancellationToken);

        second.ConversationId.ShouldBe(first.ConversationId);

        // Ikkinchi so'rovda oldingi savol-javob bor: 3 xabar (savol, javob, yangi savol).
        llm.Requests[1].Messages.Count.ShouldBe(3);
        llm.Requests[1].Messages[0].Text.ShouldBe("Birinchi savol");
        llm.Requests[1].Messages[1].Text.ShouldBe("Birinchi javob.");

        // Tarix bazada ham bor.
        int stored = await scope.Db.AiMessages.CountAsync(
            m => m.ConversationId == first.ConversationId, TestContext.Current.CancellationToken);
        stored.ShouldBe(4);
    }

    [Fact]
    public async Task Telegram_chati_boyicha_suhbat_topiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.EnqueueText("Bir.").EnqueueText("Ikki.");

        IAiGateway gateway = scope.Service<IAiGateway>();
        AiUser user = AiTestSupport.User(null, WmsPermissions.DashboardView);
        const long chatId = 777_000_111;

        AiAnswer first = await gateway.AskAsync(
            user,
            new AiAskRequest(AiChannel.Telegram, "Savol 1", TelegramChatId: chatId),
            TestContext.Current.CancellationToken);

        // Botda `conversationId` yo'q — suhbat CHAT bo'yicha topiladi (oyna ichida).
        AiAnswer second = await gateway.AskAsync(
            user,
            new AiAskRequest(AiChannel.Telegram, "Savol 2", TelegramChatId: chatId),
            TestContext.Current.CancellationToken);

        second.ConversationId.ShouldBe(first.ConversationId);
    }

    [Fact]
    public async Task Har_chaqiriq_ai_usage_ga_tushadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("today_summary"));
        llm.EnqueueText("Bugun hammasi joyida.");

        await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(null, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Holat?"),
            TestContext.Current.CancellationToken);

        // Sikl IKKI marta aylandi — hisobda ham ikkita so'rov turishi kerak.
        AiUsage usage = await scope.Db.AiUsages.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        usage.Requests.ShouldBe(2);
        usage.InputTokens.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Kabinet_va_zavod_toollari_ARALASHMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        IAiToolRegistry registry = scope.Service<IAiToolRegistry>();

        // Kabinet foydalanuvchisi: WMS ruxsatlari BO'SH, faqat `portal.self`.
        IReadOnlyList<string> portal = [.. registry.Available(Context(WmsPermissions.PortalSelf)).Select(t => t.Code)];
        portal.ShouldBe(["my_debt", "my_transfers"], ignoreOrder: true);

        // Zavod xodimi — hamma ruxsat bilan ham kabinet tool'larini KO'RMAYDI:
        // `portal.self` RBAC katalogida yo'q, ya'ni hech bir rolda uchramaydi.
        IReadOnlyList<string> staff =
            [.. registry.Available(Context([.. WmsPermissions.All.Select(p => p.Code)])).Select(t => t.Code)];

        staff.ShouldNotContain("my_debt");
        staff.ShouldNotContain("my_transfers");
        staff.ShouldContain("stock_query");
    }

    [Fact]
    public async Task Ai_ochiq_bolsa_gateway_ishlamaydi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.EnqueueText("Bu javob hech qachon kerak bo'lmaydi.");

        // `ai.chat` yoqilmagan — provayderga umuman chiqilmaydi.
        AiException error = await Should.ThrowAsync<AiException>(
            () => scope.Service<IAiGateway>().AskAsync(
                AiTestSupport.User(null, WmsPermissions.DashboardView),
                new AiAskRequest(AiChannel.Web, "Holat?"),
                TestContext.Current.CancellationToken));

        error.Code.ShouldBe(AiErrorCodes.Disabled);
        llm.Requests.ShouldBeEmpty();
    }

    /// <summary>Berilgan ruxsatlar bilan tool konteksti (feature'lar to'liq).</summary>
    private static AiToolContext Context(params string[] permissions) => new(
        AiChannel.Web,
        null,
        new HashSet<string>(permissions, StringComparer.Ordinal),
        new HashSet<string>(
            [FeatureCodes.WarehouseBatches, FeatureCodes.FinanceDebts, FeatureCodes.FinancePayments],
            StringComparer.OrdinalIgnoreCase),
        "uz");
}
