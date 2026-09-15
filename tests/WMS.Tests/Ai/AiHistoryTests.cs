using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai;

/// <summary>
/// Oqim hodisalari va suhbat tarixi (A2 yuzalari).
/// </summary>
/// <remarks>
/// ⚠️ Eng muhim da'vo — <b>begona suhbat ko'rinmaydi</b>. RLS tenantni ajratadi, lekin bir
/// tenant ichida xodimlar bir-birining savolini ko'rmasligi kerak: savolning O'ZIDA ham
/// ma'lumot bo'ladi («Korzinka qarzi qancha?»).
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class AiHistoryTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public AiHistoryTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Oqim_hodisalari_tartibda_keladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("today_summary"));
        llm.EnqueueText("Bugun hammasi joyida.");

        List<AiStreamEvent> events = [];
        await foreach (AiStreamEvent evt in scope.Service<IAiGateway>().StreamAsync(
            AiTestSupport.User(null, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Holat?"),
            TestContext.Current.CancellationToken))
        {
            events.Add(evt);
        }

        // Tool natijasi javobdan OLDIN keladi — foydalanuvchi ish borayotganini ko'rsin.
        events.Select(e => e.Type).ShouldBe([
            AiStreamEvent.Types.ToolResult,
            AiStreamEvent.Types.Text,
            AiStreamEvent.Types.Done,
        ]);

        events[0].Tool!.Code.ShouldBe("today_summary");
        events[1].Text.ShouldBe("Bugun hammasi joyida.");
        events[2].ConversationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task Rad_javobi_birinchi_hodisadan_OLDIN_keladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        // `ai.chat` yoqilmagan. Yuza SSE sarlavhalarini yozib bo'lgach holat kodini
        // o'zgartira olmaydi — shuning uchun darvoza birinchi hodisadan oldin turadi.
        AiTestSupport.ResetLlm(scope).EnqueueText("Kerak bo'lmaydi.");

        await Should.ThrowAsync<AiException>(async () =>
        {
            await foreach (AiStreamEvent _ in scope.Service<IAiGateway>().StreamAsync(
                AiTestSupport.User(null, WmsPermissions.DashboardView),
                new AiAskRequest(AiChannel.Web, "Holat?"),
                TestContext.Current.CancellationToken))
            {
                Assert.Fail("Hodisa kelmasligi kerak edi.");
            }
        });
    }

    [Fact]
    public async Task Suhbatlar_royxati_va_tafsiloti()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);

        FakeLlmClient llm = AiTestSupport.ResetLlm(scope);
        llm.Enqueue(AiTestSupport.ToolCall("today_summary"));
        llm.EnqueueText("Bugun hammasi joyida.");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(user.Id, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Bugungi holat qanday?"),
            TestContext.Current.CancellationToken);

        IAiHistory history = scope.Service<IAiHistory>();

        AiConversationDto listed = (await history.ListAsync(user.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        listed.Id.ShouldBe(answer.ConversationId);
        listed.Title.ShouldBe("Bugungi holat qanday?");
        listed.Channel.ShouldBe(AiChannel.Web);

        // Savol, model javobi (tool bilan), tool natijalari, yakuniy javob.
        listed.MessageCount.ShouldBe(4);

        AiConversationDetailDto detail = (await history.GetAsync(
            user.Id, answer.ConversationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        detail.Messages.Count.ShouldBe(4);
        detail.Messages[0].Role.ShouldBe(AiMessageRole.User);
        detail.Messages[1].Tools.ShouldBe(["today_summary"]);
        detail.Messages[^1].Text.ShouldBe("Bugun hammasi joyida.");
    }

    [Fact]
    public async Task Begona_suhbat_KORINMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        UserProfile owner = await TestData.AddUserAsync(scope.Db, "Egasi");
        UserProfile stranger = await TestData.AddUserAsync(scope.Db, "Begona");

        AiTestSupport.ResetLlm(scope).EnqueueText("Javob.");

        AiAnswer answer = await scope.Service<IAiGateway>().AskAsync(
            AiTestSupport.User(owner.Id, WmsPermissions.DashboardView),
            new AiAskRequest(AiChannel.Web, "Korzinka qarzi qancha?"),
            TestContext.Current.CancellationToken);

        IAiHistory history = scope.Service<IAiHistory>();

        (await history.ListAsync(stranger.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ShouldBeEmpty();

        (await history.GetAsync(stranger.Id, answer.ConversationId, TestContext.Current.CancellationToken))
            .ShouldBeNull();
    }
}
