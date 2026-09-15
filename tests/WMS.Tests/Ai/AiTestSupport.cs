using System.Text.Json;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai;

/// <summary>
/// AI testlari uchun umumiy tayyorgarlik.
/// </summary>
/// <remarks>
/// Nega alohida: «tenantda AI yoqilgan» holati har AI testida kerak va uni har faylda
/// takrorlash qoidaning O'ZGARISHINI (masalan feature nomi) bir joyda ushlab qolishni
/// imkonsiz qilardi.
/// </remarks>
internal static class AiTestSupport
{
    /// <summary>Console operatori qiladigan narsa: tenantga <c>ai.chat</c> override'i.</summary>
    /// <param name="scope">Tenant qamrovi.</param>
    /// <param name="tenant">Tenant.</param>
    /// <returns>Asinxron amal.</returns>
    public static async Task EnableAiAsync(WmsTenantScope scope, TestTenant tenant)
    {
        scope.Db.TenantFeatures.Add(new TenantFeature { FeatureCode = FeatureCodes.AiChat, IsEnabled = true });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Holat keshdan kelsa o'zgarish ko'rinmasdi (Console ham shuni chaqiradi).
        scope.Service<ITenantStateService>().Invalidate(tenant.Id);
    }

    /// <summary>Fake klient — singleton: har test uni o'zi uchun tozalaydi.</summary>
    /// <param name="scope">Tenant qamrovi.</param>
    /// <returns>Tozalangan klient.</returns>
    public static FakeLlmClient ResetLlm(WmsTenantScope scope)
    {
        FakeLlmClient llm = scope.Service<FakeLlmClient>();
        llm.Reset();
        return llm;
    }

    /// <summary>
    /// Qidiruvda TOPILADIGAN mahsulot yaratadi.
    /// </summary>
    /// <param name="scope">Tenant qamrovi.</param>
    /// <param name="name">Mahsulot nomi.</param>
    /// <returns>Mahsulot.</returns>
    /// <remarks>
    /// ⚠️ <c>TestData.AddProductAsync</c> <c>NameSearch</c> ni to'ldirmaydi (u katalog
    /// modulining ishi), qidiruv esa AYNAN shu ustunga qaraydi. Normalizatsiya prod
    /// servisidagi qoida bilan — <see cref="SearchNormalizer"/> orqali — qo'yiladi:
    /// qo'lda yozilgan qiymat bilan test qidiruvni emas, o'z taxminini tekshirardi.
    /// </remarks>
    public static async Task<Product> AddSearchableProductAsync(WmsTenantScope scope, string name)
    {
        Product product = await TestData.AddProductAsync(scope.Db, name);
        product.NameSearch = SearchNormalizer.Normalize(name);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }

    /// <summary>Model javobi: bitta tool chaqirig'i.</summary>
    /// <param name="tool">Tool kodi.</param>
    /// <param name="argumentsJson">Argumentlar (JSON obyekt).</param>
    /// <returns>Javob.</returns>
    public static LlmResponse ToolCall(string tool, string argumentsJson = "{}") =>
        new(
            null,
            [new LlmToolCall($"call-{Guid.NewGuid():N}", tool, JsonDocument.Parse(argumentsJson).RootElement.Clone())],
            LlmStopReason.ToolUse,
            new LlmUsage(100, 20, 0, 0),
            "claude-sonnet-5");

    /// <summary>Foydalanuvchi: berilgan ruxsatlar bilan.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="permissions">Ruxsat kodlari.</param>
    /// <returns>Gateway foydalanuvchisi.</returns>
    public static AiUser User(Guid? profileId, params string[] permissions) =>
        new(profileId, new HashSet<string>(permissions, StringComparer.Ordinal), "uz");
}
