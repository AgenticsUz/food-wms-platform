using System.Text;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai.Eval;

/// <summary>
/// Sinov to'plami — FIXTURE rejimi (CI).
/// </summary>
/// <remarks>
/// <para>
/// Bu yerda MODEL emas, WMS o'lchanadi: har savol uchun kutilgan tool o'z argumentlari
/// bilan bajariladi va natija kutilgan belgilarga solishtiriladi. Ya'ni «model to'g'ri
/// tool tanlasa, javob TO'G'RI bo'ladimi?» degan savolga javob beriladi.
/// </para>
/// <para>
/// ⚠️ Nega bu ham kerak: jonli rejim past ball bergan sayin savol tug'iladi — model
/// yanglishdimi yoki WMS noto'g'ri son berdimi? Fixture rejimi shu ikkisini AJRATADI va
/// u har commit'da, pulsiz yuradi.
/// </para>
/// <para>
/// Modelning o'zini baholash — <see cref="AiEvalLiveTests"/> (qo'lda, <c>AI_EVAL_LIVE=1</c>).
/// </para>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class AiEvalTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public AiEvalTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Toplam_rejadagi_shaklda()
    {
        IReadOnlyList<AiEvalCase> cases = AiEvalCase.Load();

        // Reja: A1 da 50 savol, A3 da +27 (qoralama 15, to'lov 12).
        cases.Count.ShouldBe(77);
        cases.Select(c => c.Id).Distinct().Count().ShouldBe(77);

        Dictionary<string, int> expected = new(StringComparer.Ordinal)
        {
            ["qoldiq"] = 10,
            ["muddat"] = 5,
            ["qarz"] = 8,
            ["bugungi"] = 5,
            ["kutilayotgan"] = 5,
            ["narx"] = 5,
            ["noaniq"] = 7,
            ["ruxsatsiz"] = 5,
            ["qoralama"] = 15,
            ["tolov"] = 12,
        };

        foreach ((string category, int count) in expected)
        {
            cases.Count(c => c.Category == category).ShouldBe(count, $"toifa '{category}'");
        }

        // Ruscha va aralash savollar ham bo'lishi SHART: mijozlarning bir qismi ruscha yozadi.
        cases.Count(c => c.Lang == "ru").ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task Ruxsatsiz_savollarda_tool_modelga_KORINMAYDI()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        IAiToolRegistry registry = scope.Service<IAiToolRegistry>();
        IReadOnlyList<AiEvalCase> cases = [.. AiEvalCase.Load().Where(c => c.ExpectRefusal)];

        // A1 da 5 ta, A3 da yana ikkitasi (qoralama va to'lov ruxsatsiz).
        cases.Count.ShouldBe(7);

        foreach (AiEvalCase test in cases)
        {
            IReadOnlyList<IAiTool> available = registry.Available(Context(test, AllFeatures()));

            available.Select(t => t.Code).ShouldNotContain(test.Tool, $"savol '{test.Id}'");

            // Va zo'rlab chaqirilsa ham rad etiladi (model nomni o'zi to'qishi mumkin).
            Should.Throw<AiException>(() => registry.Require(test.Tool, Context(test, AllFeatures())))
                .Code.ShouldBe(AiErrorCodes.ToolForbidden);
        }
    }

    [Fact]
    public async Task Har_savol_uchun_kutilgan_tool_TOGRI_javob_beradi()
    {
        TestTenant tenant = await _fixture.CreateTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        await AiTestSupport.EnableAiAsync(scope, tenant);

        Domain.Entities.UserProfile user = await TestData.AddUserAsync(scope.Db);
        scope.AsUser(user.Id, WmsPermissions.All.Select(p => p.Code).ToArray());

        await AiEvalDataset.SeedAsync(scope, user.Id);

        IAiToolRegistry registry = scope.Service<IAiToolRegistry>();
        IReadOnlySet<string> features = AllFeatures();

        List<string> failures = [];

        foreach (AiEvalCase test in AiEvalCase.Load().Where(c => !c.ExpectRefusal))
        {
            AiToolContext context = Context(test, features);

            // 1. Savol uchun kerakli tool SHU foydalanuvchiga ko'rinadimi.
            IReadOnlyList<string> offered = [.. registry.Available(context).Select(t => t.Code)];
            if (!offered.Contains(test.Tool))
            {
                failures.Add($"{test.Id}: '{test.Tool}' tool'i ruxsatlar bilan ko'rinmadi ({string.Join(", ", test.Permissions)})");
                continue;
            }

            // 2. Tool o'z argumentlari bilan bajarilsa, kutilgan javobni beradimi.
            IAiTool tool = registry.Require(test.Tool, context);
            AiToolResult result = await tool.ExecuteAsync(test.Args, context, TestContext.Current.CancellationToken);

            if (result.IsError)
            {
                failures.Add($"{test.Id}: tool xato qaytardi — {result.Text}");
                continue;
            }

            foreach (string marker in test.Markers)
            {
                if (!result.Text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{test.Id}: javobda '{marker}' yo'q. Javob: {Shorten(result.Text)}");
                }
            }

            // 3. Noaniq nomda javob TANLAMAYDI, so'raydi.
            if (test.ExpectClarify && !LooksLikeClarify(result.Text))
            {
                failures.Add($"{test.Id}: noaniq nom edi, lekin javob savol emas: {Shorten(result.Text)}");
            }
        }

        if (failures.Count > 0)
        {
            StringBuilder report = new($"Sinov to'plamida {failures.Count} nuqson:");
            foreach (string failure in failures)
            {
                report.Append("\n  - ").Append(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    /// <summary>Javob «tanlab qo'ydi» emas, «so'rayapti»mi.</summary>
    /// <remarks>
    /// Belgilar <c>AiNameResolver</c> matnidan: nomzodlar sanaladi va tanlov foydalanuvchiga
    /// qoldiriladi. Matn o'zgarsa test qizil beradi — bu ATAYLAB, chunki o'sha matn
    /// modelning savol berishini qo'zg'atadigan yagona narsa.
    /// </remarks>
    private static bool LooksLikeClarify(string text) =>
        text.Contains("nomzod", StringComparison.OrdinalIgnoreCase)
        || text.Contains(" ta mahsulot", StringComparison.OrdinalIgnoreCase)
        || text.Contains(" ta kontragent", StringComparison.OrdinalIgnoreCase)

        // A3 qoralamalari: yetishmayotgan ma'lumot («kontragent yo'q», «ombor
        // aniqlanmadi», «yo'nalish ko'rsatilmagan») doim «so'rang» bilan tugaydi.
        || text.Contains("so'rang", StringComparison.OrdinalIgnoreCase);

    private static AiToolContext Context(AiEvalCase test, IReadOnlySet<string> features) => new(
        AiChannel.Web,
        null,
        new HashSet<string>(test.Permissions, StringComparer.Ordinal),
        features,
        test.Lang == "ru" ? "ru" : "uz");

    /// <summary>Hamma feature yoqilgan — sinov to'plami RUXSATNI o'lchaydi, tarifni emas.</summary>
    private static IReadOnlySet<string> AllFeatures() => new HashSet<string>(
        [
            FeatureCodes.WarehouseStock, FeatureCodes.WarehouseBatches, FeatureCodes.FinanceDebts,
            FeatureCodes.FinancePayments, FeatureCodes.FinanceTransactions, FeatureCodes.AiChat,
            FeatureCodes.TransfersIncoming, FeatureCodes.TransfersOutgoing,
        ],
        StringComparer.OrdinalIgnoreCase);

    private static string Shorten(string text) =>
        text.Length <= 160 ? text.ReplaceLineEndings(" ") : text.ReplaceLineEndings(" ")[..157] + "...";
}
