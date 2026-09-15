using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Domain.Enums;
using WMS.Infrastructure.Services.Ai;

namespace WMS.Tests.Ai;

/// <summary>
/// Tool registri — AI qatlamining XAVFSIZLIK CHEGARASI (F10 §0.3).
/// </summary>
/// <remarks>
/// <para>
/// Bu yerdagi da'volar prompt bilan emas, KOD bilan qo'riqlanadi: ruxsati yo'q amal modelga
/// umuman ko'rsatilmaydi va ko'rsatilmagan amalni chaqirish rad etiladi. Shuning uchun
/// «ko'rinmaydi» ni tekshirish yetarli emas — «chaqirib ham bo'lmaydi» alohida tekshiriladi:
/// model nomni o'zi to'qib chaqirishi mumkin.
/// </para>
/// <para>
/// Baza kerak emas: registr faqat kontekstdagi to'plamlarga qaraydi.
/// </para>
/// </remarks>
public sealed class AiToolRegistryTests
{
    private static readonly AiToolContext Viewer = Context(WmsPermissions.WarehouseView);

    [Fact]
    public void Ruxsati_yoq_tool_modelga_korinmaydi()
    {
        AiToolRegistry registry = Build(
            new StubTool("stock_query", WmsPermissions.WarehouseView),
            new StubTool("debt_query", WmsPermissions.FinanceView));

        IReadOnlyList<IAiTool> available = registry.Available(Viewer);

        available.Select(t => t.Code).ShouldBe(["stock_query"]);
    }

    [Fact]
    public void Feature_ochiq_bolsa_tool_korinmaydi()
    {
        AiToolRegistry registry = Build(
            new StubTool("stock_query", WmsPermissions.WarehouseView),
            new StubTool("expiry_query", WmsPermissions.WarehouseView, FeatureCodes.WarehouseBatches));

        // Ruxsat BOR, lekin tenant partiyalarni sotib olmagan.
        AiToolContext context = Context(WmsPermissions.WarehouseView);

        registry.Available(context).Select(t => t.Code).ShouldBe(["stock_query"]);

        // O'sha kontekst feature bilan — ikkalasi ham ko'rinadi.
        AiToolContext withFeature = Context(
            [WmsPermissions.WarehouseView],
            [FeatureCodes.WarehouseBatches]);

        registry.Available(withFeature).Select(t => t.Code).ShouldBe(["expiry_query", "stock_query"]);
    }

    [Fact]
    public void Korinmagan_tool_chaqirilsa_rad_etiladi()
    {
        AiToolRegistry registry = Build(
            new StubTool("stock_query", WmsPermissions.WarehouseView),
            new StubTool("debt_query", WmsPermissions.FinanceView));

        // Model ro'yxatda ko'rmagan tool'ni nomi bilan chaqirdi.
        AiException error = Should.Throw<AiException>(() => registry.Require("debt_query", Viewer));

        error.Code.ShouldBe(AiErrorCodes.ToolForbidden);
        error.Status.ShouldBe(403);
    }

    [Fact]
    public void Umuman_yoq_tool_ham_rad_etiladi()
    {
        AiToolRegistry registry = Build(new StubTool("stock_query", WmsPermissions.WarehouseView));

        Should.Throw<AiException>(() => registry.Require("delete_everything", Viewer))
            .Code.ShouldBe(AiErrorCodes.ToolForbidden);
    }

    [Fact]
    public void Takror_kod_startupda_yiqitadi()
    {
        // Jimgina «oxirgisi yutadi» bo'lsa, modelga ko'rinadigan nom ortida kutilmagan amal
        // turishi mumkin edi — ruxsat chegarasini aylanib o'tishning eng qisqa yo'li.
        Should.Throw<InvalidOperationException>(() => Build(
            new StubTool("stock_query", WmsPermissions.WarehouseView),
            new StubTool("stock_query", WmsPermissions.FinanceView)));
    }

    private static AiToolRegistry Build(params IAiTool[] tools) =>
        new(tools, NullLogger<AiToolRegistry>.Instance);

    private static AiToolContext Context(params string[] permissions) => Context(permissions, []);

    private static AiToolContext Context(string[] permissions, string[] features) =>
        new(
            AiChannel.Web,
            Guid.CreateVersion7(),
            new HashSet<string>(permissions, StringComparer.Ordinal),
            new HashSet<string>(features, StringComparer.OrdinalIgnoreCase),
            "uz");

    /// <summary>Bajarilmaydigan tool: registr faqat metama'lumotga qaraydi.</summary>
    private sealed class StubTool : IAiTool
    {
        public StubTool(string code, string permission, string? feature = null)
        {
            Code = code;
            PermissionCode = permission;
            FeatureCode = feature;
        }

        public string Code { get; }

        public string Description => Code;

        public JsonElement Schema => AiToolSchema.Empty;

        public string PermissionCode { get; }

        public string? FeatureCode { get; }

        public Task<AiToolResult> ExecuteAsync(
            JsonElement arguments, AiToolContext context, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Registr testi tool'ni bajarmaydi.");
    }
}
