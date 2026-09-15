using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai;

/// <summary>
/// Yozuvchi amallar (A3): qoralama tayyorlanadi, YOZUV yaratilmaydi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Eng muhim ikki da'vo: <b>AI hujjat yarata olmaydi</b> va <b>tasdiqlash tool'i
/// umuman mavjud emas</b> (F10 §0.6). Ikkinchisi statik darvoza: kimdir kelajakda
/// «qulaylik uchun» <c>confirm_transfer</c> qo'shsa, test darhol qizil beradi.
/// </para>
/// <para>
/// Uchinchi da'vo — to'lovda yo'nalish TAXMIN qilinmasligi: `CreatePaymentDto` da u
/// ixtiyoriy va bo'sh bo'lsa qarz belgisidan chiqariladi. Odam uchun qulay, AI uchun
/// xavfli — nol balansda summa teskari tomonga yozilardi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class AiDraftTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public AiDraftTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Tasdiqlovchi_tool_UMUMAN_YOQ()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        IAiToolRegistry registry = scope.Service<IAiToolRegistry>();
        AiToolContext everything = Context([.. WmsPermissions.All.Select(p => p.Code)]);

        IReadOnlyList<string> codes = [.. registry.Available(everything).Select(t => t.Code)];

        // Hamma ruxsat bilan ham yozuvchi/tasdiqlovchi tool yo'q.
        codes.ShouldNotContain(code => code.StartsWith("confirm_", StringComparison.Ordinal), "tasdiqlovchi tool");
        codes.ShouldNotContain(code => code.StartsWith("create_", StringComparison.Ordinal), "yozuvchi tool");
        codes.ShouldNotContain(code => code.StartsWith("delete_", StringComparison.Ordinal), "o'chiruvchi tool");

        // Model nomni o'zi to'qib chaqirsa ham rad etiladi.
        foreach (string invented in new[] { "confirm_transfer", "confirm_payment", "create_payment" })
        {
            Should.Throw<AiException>(() => registry.Require(invented, everything))
                .Code.ShouldBe(AiErrorCodes.ToolForbidden);
        }
    }

    [Fact]
    public async Task HAR_toolning_sxemasi_quriladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        // ⚠️ Bu darvoza jonli sinovda topilgan nuqsondan keyin qo'shildi: sxema
        // eksporti `JsonSerializerOptions` ni faqat o'qishga belgilaydi va resolver
        // oshkora berilmagan bo'lsa yiqiladi. Nuqson TARTIBGA bog'liq edi — biror
        // tool avval BAJARILGAN bo'lsa sozlamalar allaqachon to'lgan bo'lardi va
        // to'plam yashil qolardi. Gateway esa sxemani BIRINCHI so'raydi.
        AiToolContext everything = Context([.. WmsPermissions.All.Select(p => p.Code), WmsPermissions.PortalSelf]);

        IReadOnlyList<IAiTool> tools = scope.Service<IAiToolRegistry>().Available(everything);
        tools.Count.ShouldBeGreaterThan(10);

        foreach (IAiTool tool in tools)
        {
            JsonElement schema = tool.Schema;
            schema.GetProperty("type").GetString().ShouldBe("object", $"tool '{tool.Code}'");
        }
    }

    [Fact]
    public async Task Qoralama_hujjat_YARATMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy sklad");
        Product product = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");
        await TestData.AddStockAsync(scope.Db, warehouse, product, 100);
        await AddCounterpartyAsync(scope, "Korzinka");

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("draft_transfer", """
            {"type":"outgoing","counterpartyName":"Korzinka",
             "items":[{"productName":"Snikers","quantity":10,"unitPrice":12000}]}
            """));
        llm.EnqueueText("Qoralama tayyor, tasdiqlaysizmi?");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            User(WmsPermissions.TransfersCreate, WmsPermissions.ProductsView, WmsPermissions.PartnersView),
            new AiAskRequest(AiChannel.Web, "Korzinkaga 10 dona Snikers"),
            TestContext.Current.CancellationToken);

        answer.Tools.ShouldHaveSingleItem().Code.ShouldBe("draft_transfer");

        // ⚠️ Eng muhimi: baza O'ZGARMAGAN.
        (await scope.Db.Transfers.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Narx_berilmasa_oxirgi_narx_qoyiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        scope.AsUser(user.Id, [.. WmsPermissions.All.Select(p => p.Code)]);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy sklad");
        Product product = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");
        await TestData.AddStockAsync(scope.Db, warehouse, product, 100);
        Counterparty korzinka = await AddCounterpartyAsync(scope, "Korzinka");

        // Tasdiqlangan sotuv — oxirgi narx shundan olinadi.
        ITransferService transfers = scope.Service<ITransferService>();
        TransferDto sale = await transfers.CreateAsync(user.Id, new CreateTransferDto
        {
            Type = TransferType.Outgoing,
            FromWarehouseId = warehouse.Id,
            CounterpartyId = korzinka.Id,
            Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 1, UnitPrice = 12_000m }],
        });
        await transfers.ConfirmAsync(sale.Id);

        AiTransferDraft draft = await DraftAsync(scope, """
            {"type":"outgoing","counterpartyName":"Korzinka",
             "items":[{"productName":"Snikers","quantity":5}]}
            """);

        AiDraftItem item = draft.Items.ShouldHaveSingleItem();
        item.UnitPrice.ShouldBe(12_000m);
        item.PriceSource.ShouldBe("last_price");
        item.Shortfall.ShouldBeFalse();
    }

    [Fact]
    public async Task Qoldiq_yetmasa_qoralama_QAYTADI_lekin_belgilanadi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy sklad");
        Product product = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");
        await TestData.AddStockAsync(scope.Db, warehouse, product, 3);
        await AddCounterpartyAsync(scope, "Korzinka");

        AiTransferDraft draft = await DraftAsync(scope, """
            {"type":"outgoing","counterpartyName":"Korzinka",
             "items":[{"productName":"Snikers","quantity":10,"unitPrice":12000}]}
            """);

        // «Yetmaydi» — foydalanuvchi ko'rishi kerak bo'lgan HOLAT, qoralamani tashlab
        // yuborish sababi emas: u miqdorni tuzatib yuborishi mumkin.
        draft.HasShortfall.ShouldBeTrue();
        draft.Items[0].Available.ShouldBe(3);
        draft.Items[0].Quantity.ShouldBe(10);
    }

    [Fact]
    public async Task Qadoq_asosiy_birlikka_ogiriladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy sklad");
        Product product = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");

        // 1 quti = 12 dona (P2.7 Variant A).
        product.PackSize = 12m;
        product.PackUnit = "quti";
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await TestData.AddStockAsync(scope.Db, warehouse, product, 1000);
        await AddCounterpartyAsync(scope, "Korzinka");

        AiTransferDraft draft = await DraftAsync(scope, """
            {"type":"outgoing","counterpartyName":"Korzinka",
             "items":[{"productName":"Snikers","packs":50,"unitPrice":12000}]}
            """);

        // 50 quti → 600 dona. Qoldiq va FEFO DOIM asosiy birlikda.
        draft.Items.ShouldHaveSingleItem().Quantity.ShouldBe(600m);
    }

    [Fact]
    public async Task Tolovda_yonalish_TAXMIN_QILINMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        await AddCounterpartyAsync(scope, "Korzinka");

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("draft_payment", """
            {"counterpartyName":"Korzinka","amount":2000000}
            """));
        llm.EnqueueText("Yo'nalishni aniqlashtiring.");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            User(WmsPermissions.FinanceManage, WmsPermissions.PartnersView),
            new AiAskRequest(AiChannel.Web, "Korzinka 2 mln to'ladi"),
            TestContext.Current.CancellationToken);

        // Yo'nalishsiz qoralama TAYYORLANMAYDI — model so'rashi kerak.
        AiToolOutput output = answer.Tools.ShouldHaveSingleItem();
        output.Data.ShouldBeNull();
        output.Text.ShouldContain("Yo'nalish");
    }

    [Fact]
    public async Task Tolov_qoralamasi_balans_oldi_keyin_beradi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        Counterparty korzinka = await AddCounterpartyAsync(scope, "Korzinka");
        scope.Db.Debts.Add(new Debt { CounterpartyId = korzinka.Id, Amount = 5_400_000m });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("draft_payment", """
            {"counterpartyName":"Korzinka","amount":2000000,"direction":"in"}
            """));
        llm.EnqueueText("Tasdiqlaysizmi?");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            User(WmsPermissions.FinanceManage, WmsPermissions.PartnersView),
            new AiAskRequest(AiChannel.Web, "Korzinka 2 mln berdi, qarzidan yop"),
            TestContext.Current.CancellationToken);

        AiPaymentDraft draft = answer.Tools.ShouldHaveSingleItem().Data.ShouldBeOfType<AiPaymentDraft>();

        draft.BalanceBefore.ShouldBe(5_400_000m);
        draft.BalanceAfter.ShouldBe(3_400_000m);
        draft.Direction.ShouldBe(PaymentDirection.In);

        // Yozuv YARATILMAGAN.
        (await scope.Db.PaymentHistories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Tasdiqlangan_qoralama_manba_va_suhbat_bilan_yoziladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        scope.AsUser(user.Id, [.. WmsPermissions.All.Select(p => p.Code)]);

        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db, "Asosiy sklad");
        Product product = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");
        await TestData.AddStockAsync(scope.Db, warehouse, product, 100);
        Counterparty korzinka = await AddCounterpartyAsync(scope, "Korzinka");

        // Suhbat — audit izining ikkinchi yarmi.
        AiTestSupport.ResetLlm(scope).EnqueueText("Salom");
        AiAnswer chat = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(user.Id, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Salom"),
            TestContext.Current.CancellationToken);

        // Yuza (tugma) qiladigan ish: manba va suhbat SERVER tomonda qo'yiladi.
        TransferDto created = await scope.Service<ITransferService>().CreateAsync(
            user.Id,
            new CreateTransferDto
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = warehouse.Id,
                CounterpartyId = korzinka.Id,
                Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 5, UnitPrice = 12_000m }],
            },
            DocumentSource.Ai,
            chat.ConversationId);

        created.Status.ShouldBe(TransferStatus.Pending);
        created.Source.ShouldBe(DocumentSource.Ai);

        Transfer row = await scope.Db.Transfers.AsNoTracking()
            .SingleAsync(t => t.Id == created.Id, TestContext.Current.CancellationToken);

        row.AiConversationId.ShouldBe(chat.ConversationId);
    }

    /// <summary>Qoralamani gateway orqali oladi (model uni chaqirgan deb).</summary>
    private async Task<AiTransferDraft> DraftAsync(WmsTenantScope scope, string argumentsJson)
    {
        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("draft_transfer", argumentsJson));
        llm.EnqueueText("Qoralama tayyor.");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            User(WmsPermissions.TransfersCreate, WmsPermissions.ProductsView, WmsPermissions.PartnersView),
            new AiAskRequest(AiChannel.Web, "Qoralama tayyorla"),
            TestContext.Current.CancellationToken);

        AiToolOutput output = answer.Tools.ShouldHaveSingleItem();
        return output.Data.ShouldBeOfType<AiTransferDraft>();
    }

    private static async Task<Counterparty> AddCounterpartyAsync(WmsTenantScope scope, string name)
    {
        Counterparty counterparty = await TestData.AddCounterpartyAsync(scope.Db, name);
        counterparty.NameSearch = SearchNormalizer.Normalize(name);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return counterparty;
    }

    private static AiUser User(params string[] permissions) => AiTestSupport.User(null, permissions);

    private static AiToolContext Context(params string[] permissions) => new(
        AiChannel.Web,
        null,
        new HashSet<string>(permissions, StringComparer.Ordinal),
        new HashSet<string>(
            [
                FeatureCodes.TransfersIncoming, FeatureCodes.TransfersOutgoing,
                FeatureCodes.FinancePayments, FeatureCodes.FinanceDebts, FeatureCodes.WarehouseBatches,
            ],
            StringComparer.OrdinalIgnoreCase),
        "uz");
}
