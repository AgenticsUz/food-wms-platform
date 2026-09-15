using WMS.Application.Common.Localization;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Infrastructure.Services.Operations.Telegram;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai;

/// <summary>
/// Botdagi erkin matn AI'ga o'tadigan yo'l (F10·A1).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Eng muhim da'vo — <b>ULANMAGAN chat eski xatti-harakatda qoladi</b>: AI qo'shilgani
/// uchun hech kimning boti «ishlamay qolgandek» ko'rinmasin.
/// </para>
/// <para>
/// Telegram API'ga chiqilmaydi (<see cref="FakeTelegramService"/>), model ham fixture'dan —
/// bu yerda OQIM o'lchanadi: kim so'radi, qaysi tenantda, javob qanday yuborildi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class TelegramAiTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public TelegramAiTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ulanmagan_chat_AI_ga_TUSHMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        AiTestSupport.ResetLlm(scope);
        FakeTelegramService telegram = Telegram(scope);

        // Ulanish yo'q — `false`, ya'ni chaqiruvchi yo'riqnomani ko'rsatadi.
        bool handled = await scope.Service<TelegramAiCommands>()
            .HandleTextAsync(Text(UnlinkedChatId(), "Qoldiq qancha?"), TestContext.Current.CancellationToken);

        handled.ShouldBeFalse();
        telegram.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Ulangan_chatda_javob_yuboriladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        long chatId = await LinkAsync(scope);
        FakeTelegramService telegram = Telegram(scope);
        AiTestSupport.ResetLlm(scope).EnqueueText("Omborda 34 dona bor.");

        bool handled = await scope.Service<TelegramAiCommands>()
            .HandleTextAsync(Text(chatId, "Snikers qancha qoldi?"), TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        telegram.Sent.ShouldHaveSingleItem().ShouldContain("34 dona");

        // Javob bir necha soniya oladi — jimlik «bot o'lgan» degan taassurot bermasin.
        telegram.TypingCount.ShouldBe(1);
    }

    [Fact]
    public async Task Uzun_javob_bolib_yuboriladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        long chatId = await LinkAsync(scope);
        FakeTelegramService telegram = Telegram(scope);

        // Telegram 4096 belgidan uzun xabarni RAD ETADI — kesilmaydi, bo'linadi.
        string longAnswer = string.Join("\n", Enumerable.Range(1, 400).Select(i => $"{i}-qator matni"));
        AiTestSupport.ResetLlm(scope).EnqueueText(longAnswer);

        await scope.Service<TelegramAiCommands>()
            .HandleTextAsync(Text(chatId, "Hammasini ayt"), TestContext.Current.CancellationToken);

        telegram.Sent.Count.ShouldBeGreaterThan(1);
        telegram.Sent.ShouldAllBe(m => m.Length <= 4096);

        // Oxiri YO'QOLMAYDI: kesib yuborish javobning xulosasini olib ketardi.
        string.Concat(telegram.Sent).ShouldContain("400-qator");
    }

    [Fact]
    public async Task Ai_ochiq_bolsa_sababi_aytiladi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        // `ai.chat` YOQILMAGAN — foydalanuvchi jim qolmaydi, sababini oladi.
        long chatId = await LinkAsync(scope);
        FakeTelegramService telegram = Telegram(scope);
        AiTestSupport.ResetLlm(scope).EnqueueText("Bu javob kerak bo'lmaydi.");

        bool handled = await scope.Service<TelegramAiCommands>()
            .HandleTextAsync(Text(chatId, "Qarzlar qancha?"), TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        telegram.Sent.ShouldHaveSingleItem()
            .ShouldBe(Translations.Format(Messages.AiDisabled, "uz"));
    }

    private static FakeTelegramService Telegram(WmsTenantScope scope)
    {
        FakeTelegramService telegram = scope.Service<FakeTelegramService>();
        telegram.Reset();
        return telegram;
    }

    /// <summary>Chatni tenantdagi xodimga ulaydi.</summary>
    private static async Task<long> LinkAsync(WmsTenantScope scope)
    {
        UserProfile user = await TestData.AddUserAsync(scope.Db);
        long chatId = Random.Shared.NextInt64(100_000_000, 999_999_999);

        scope.Db.TelegramLinks.Add(new TelegramLink
        {
            UserProfileId = user.Id,
            ChatId = chatId,
            Lang = "uz",
            IsActive = true,
        });

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return chatId;
    }

    /// <summary>Hech qayerga ulanmagan chat.</summary>
    private static long UnlinkedChatId() => Random.Shared.NextInt64(1_000_000_000, 1_999_999_999);

    private static TelegramUpdate Text(long chatId, string text) => new(
        UpdateId: 1,
        Kind: TelegramUpdateKind.Text,
        ChatId: chatId,
        IsPrivate: true,
        FromId: chatId,
        Username: null,
        FirstName: null,
        LanguageCode: "uz",
        Command: null,
        Payload: null,
        Text: text);
}
