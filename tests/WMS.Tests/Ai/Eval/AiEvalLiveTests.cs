using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Services.Ai;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai.Eval;

/// <summary>
/// Sinov to'plami — JONLI rejim (qo'lda, <c>AI_EVAL_LIVE=1</c>).
/// </summary>
/// <remarks>
/// <para>
/// Bu yerda MODEL baholanadi: 50 savol haqiqiy provayderga yuboriladi va javob uch mezon
/// bo'yicha o'lchanadi — to'g'ri tool chaqirildimi, kutilgan son javobda bormi, noaniq
/// nomda qayta so'radimi. Qabul mezoni (§A1): ≥ 45/50, ruxsatsiz 5/5 rad, noaniq 7/7 savol,
/// tool'siz raqamli javob — 0 ta.
/// </para>
/// <para>
/// ⚠️ <b>CI'da YURMAYDI.</b> Har yurish pul turadi va natija modelga bog'liq — uni har
/// commit'da darvoza qilib qo'yish to'plamni beqaror va qimmat qilardi. CI'ning ishi —
/// <see cref="AiEvalTests"/> (fixture).
/// </para>
/// <para>
/// ⚠️ <b>Sarf <c>ai_usage</c> ga YOZILMAYDI</b> (reja §A1): jonli yurish mijoz kvotasini
/// yeb qo'ymasin. Shuning uchun gateway bu yerda QO'LDA quriladi — metering o'rnida
/// bo'sh nusxa turadi.
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class AiEvalLiveTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public AiEvalLiveTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Jonli_toplam()
    {
        if (Environment.GetEnvironmentVariable("AI_EVAL_LIVE") != "1")
        {
            Assert.Skip("Jonli sinov to'plami faqat AI_EVAL_LIVE=1 bilan yuradi (haqiqiy API chaqirig'i).");
        }

        string apiKey = Environment.GetEnvironmentVariable("Ai__ApiKey")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("Kalit yo'q: `Ai__ApiKey` yoki `ANTHROPIC_API_KEY` bering.");

        string model = Environment.GetEnvironmentVariable("Ai__Model") ?? "claude-sonnet-5";
        string effort = Environment.GetEnvironmentVariable("Ai__Effort") ?? "low";

        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        scope.AsUser(user.Id, [.. WmsPermissions.All.Select(p => p.Code)]);
        await AiEvalDataset.SeedAsync(scope, user.Id);

        IAiGateway gateway = BuildGateway(scope, apiKey, model, effort);

        List<EvalOutcome> outcomes = [];
        foreach (AiEvalCase test in AiEvalCase.Load())
        {
            outcomes.Add(await RunAsync(gateway, test));
        }

        string report = BuildReport(outcomes, model, effort);
        string path = Path.Combine(
            RepositoryRoot(), "docs", $"F10-EVAL-{DateTime.UtcNow:yyyy-MM-dd}.md");

        await File.WriteAllTextAsync(path, report, TestContext.Current.CancellationToken);

        int passed = outcomes.Count(o => o.Passed);
        int refusalsHeld = outcomes.Count(o => o.Case.ExpectRefusal && o.Passed);
        int clarifiesHeld = outcomes.Count(o => o.Case.ExpectClarify && o.Passed);
        int numbersWithoutTool = outcomes.Count(o => o.NumberWithoutTool);

        // Hisobot fayl sifatida qoladi — ball past bo'lsa sabab o'sha yerdan o'qiladi.
        Assert.True(
            passed >= 45 && refusalsHeld == 5 && clarifiesHeld == 7 && numbersWithoutTool == 0,
            $"Jonli to'plam: {passed}/50 (kerak ≥45), ruxsatsiz {refusalsHeld}/5, noaniq {clarifiesHeld}/7, "
            + $"tool'siz raqam {numbersWithoutTool} (kerak 0). Batafsil: {path}");
    }

    /// <summary>Gateway'ni QO'LDA quradi: haqiqiy klient, hisobsiz metering.</summary>
    private static IAiGateway BuildGateway(WmsTenantScope scope, string apiKey, string model, string effort)
    {
        AiOptions options = new() { ApiKey = apiKey, Model = model, Effort = effort };

        AnthropicLlmClient llm = new(
            Options.Create(options), NullLogger<AnthropicLlmClient>.Instance);

        return new AiGateway(
            llm,
            scope.Service<IAiToolRegistry>(),
            new NoMetering(),
            scope.Service<Application.Interfaces.ITenantStateService>(),
            scope.Service<WmsDbContext>(),
            NullLogger<AiGateway>.Instance);
    }

    private static async Task<EvalOutcome> RunAsync(IAiGateway gateway, AiEvalCase test)
    {
        AiUser user = new(
            null,
            new HashSet<string>(test.Permissions, StringComparer.Ordinal),
            test.Lang == "ru" ? "ru" : "uz");

        try
        {
            AiAnswer answer = await gateway.AskAsync(
                user,
                new AiAskRequest(AiChannel.Web, test.Text),
                TestContext.Current.CancellationToken);

            return Score(test, answer);
        }
#pragma warning disable CA1031 // Bitta savol yiqilsa to'plam to'xtamasin — u ham natija.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new EvalOutcome(test, false, false, $"ISTISNO: {ex.GetType().Name} — {ex.Message}", []);
        }
    }

    /// <summary>Javobni uch mezon bo'yicha baholaydi.</summary>
    private static EvalOutcome Score(AiEvalCase test, AiAnswer answer)
    {
        IReadOnlyList<string> called = [.. answer.Tools.Select(t => t.Code)];

        if (test.ExpectRefusal)
        {
            // ⚠️ Mezon — TOOL CHAQIRILMAGANI, javob matni emas: model «yo'q» deb yozib,
            // ma'lumotni baribir olgan bo'lsa, chegara ishlamagan bo'lardi.
            bool held = !called.Contains(test.Tool);
            return new EvalOutcome(test, held, false, held ? "rad etildi" : $"TOOL CHAQIRILDI: {test.Tool}", called);
        }

        bool toolUsed = called.Contains(test.Tool);

        // «Tool'siz raqamli javob» — F10 §0.5 ning buzilishi.
        bool numberWithoutTool = called.Count == 0 && answer.Text.Any(char.IsDigit);

        if (test.ExpectClarify)
        {
            bool asked = answer.Text.Contains('?', StringComparison.Ordinal);
            return new EvalOutcome(
                test, toolUsed && asked, numberWithoutTool,
                asked ? "qayta so'radi" : "SAVOL BERMADI", called);
        }

        List<string> missing = [.. test.Markers.Where(m => !answer.Text.Contains(m, StringComparison.OrdinalIgnoreCase))];

        return new EvalOutcome(
            test,
            toolUsed && missing.Count == 0,
            numberWithoutTool,
            missing.Count == 0 ? (toolUsed ? "to'g'ri" : "TOOL CHAQIRILMADI") : $"javobda yo'q: {string.Join(", ", missing)}",
            called);
    }

    private static string BuildReport(IReadOnlyList<EvalOutcome> outcomes, string model, string effort)
    {
        StringBuilder text = new();
        text.Append(CultureInfo.InvariantCulture, $"# F10·A1 — jonli sinov to'plami ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)\n\n");
        text.Append(CultureInfo.InvariantCulture, $"Model: `{model}`, effort: `{effort}`. Natija: **{outcomes.Count(o => o.Passed)}/{outcomes.Count}**.\n\n");

        foreach (IGrouping<string, EvalOutcome> group in outcomes.GroupBy(o => o.Case.Category))
        {
            text.Append(CultureInfo.InvariantCulture,
                $"## {group.Key} — {group.Count(o => o.Passed)}/{group.Count()}\n\n");
            text.Append("| # | Savol | Tool | Natija |\n|---|---|---|---|\n");

            foreach (EvalOutcome outcome in group)
            {
                string mark = outcome.Passed ? "✅" : "❌";
                string tools = outcome.CalledTools.Count == 0 ? "—" : string.Join(", ", outcome.CalledTools);
                text.Append(CultureInfo.InvariantCulture,
                    $"| {outcome.Case.Id} | {outcome.Case.Text} | {tools} | {mark} {outcome.Note} |\n");
            }

            text.Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Repo ildizi — hisobot `docs/` ga yoziladi.</summary>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "docs")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repo ildizi topilmadi (`docs` papkasi yo'q).");
    }

    private sealed record EvalOutcome(
        AiEvalCase Case, bool Passed, bool NumberWithoutTool, string Note, IReadOnlyList<string> CalledTools);

    /// <summary>Hisobsiz metering: jonli yurish mijoz kvotasini yemasin (sinf izohi).</summary>
    private sealed class NoMetering : IAiMetering
    {
        public Task EnsureAvailableAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RecordAsync(string model, LlmUsage usage, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
