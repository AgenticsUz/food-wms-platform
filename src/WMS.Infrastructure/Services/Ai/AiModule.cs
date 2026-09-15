using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Ai;
using WMS.Infrastructure.Services.Ai.Tools;

namespace WMS.Infrastructure.Services.Ai;

/// <summary>
/// F10·A0 — AI poydevori: provayder klienti, tool registri va sarf hisobi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Modul kalit BO'LMASA HAM ro'yxatdan o'tadi. Sabab: «AI o'chiq» javobini ham kimdir
/// berishi kerak — <see cref="IAiMetering.EnsureAvailableAsync"/> uni <c>ai_disabled</c>
/// bilan qaytaradi. Ro'yxatdan chiqarib qo'yilsa, yuzalar servis topolmay 500 berardi.
/// </para>
/// <para>
/// Tool'lar SHU YERGA qo'shiladi (A1 dan boshlab): <c>services.AddScoped&lt;IAiTool, …&gt;()</c>.
/// Registr ularni DI'dan yig'adi, ya'ni yangi tool qo'shish bitta qator.
/// </para>
/// </remarks>
public static class AiModule
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Singleton: SDK klienti ichida `HttpClient` tutadi (sababi `AnthropicLlmClient` da).
        services.AddSingleton<ILlmClient, AnthropicLlmClient>();

        // Registr holatsiz, lekin tool'lar servislarga (va demak `WmsDbContext` ga) tayanadi —
        // shuning uchun scoped: singleton registr scoped tool'larni ushlab qolardi.
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();

        services.AddScoped<IAiMetering, AiMetering>();
        services.AddScoped<IAiGateway, AiGateway>();
        services.AddScoped<IAiHistory, AiHistory>();

        AddTools(services);

        return services;
    }

    /// <summary>
    /// O'quvchi tool'lar (A1).
    /// </summary>
    /// <remarks>
    /// ⚠️ Har biri <c>IAiTool</c> sifatida ro'yxatdan o'tadi — registr ularni DI'dan yig'adi.
    /// Ro'yxatdan o'tmagan tool modelga umuman ko'rinmaydi, ya'ni bu yerdagi qator —
    /// amalning YOQILISHI. Ruxsat va feature esa tool'ning o'zida e'lon qilinadi.
    /// </remarks>
    private static void AddTools(IServiceCollection services)
    {
        services.AddScoped<IAiTool, FindProductTool>();
        services.AddScoped<IAiTool, FindCounterpartyTool>();
        services.AddScoped<IAiTool, StockQueryTool>();
        services.AddScoped<IAiTool, ExpiryQueryTool>();
        services.AddScoped<IAiTool, DebtQueryTool>();
        services.AddScoped<IAiTool, PaymentHistoryTool>();
        services.AddScoped<IAiTool, FinanceSummaryTool>();
        services.AddScoped<IAiTool, PendingTransfersTool>();
        services.AddScoped<IAiTool, LastPriceTool>();
        services.AddScoped<IAiTool, TodaySummaryTool>();

        // Yozuvchi amallar (A3) — QORALAMA tayyorlaydi, hujjat yozmaydi. Yozuvni odam
        // tugma bosganda yuza yaratadi (`confirm_transfer`/`confirm_payment` tool'i YO'Q).
        services.AddScoped<IAiTool, DraftTransferTool>();
        services.AddScoped<IAiTool, DraftPaymentTool>();

        // Kabinet (A2): ruxsat kodi `portal.self` — u RBAC katalogida yo'q, ya'ni bu
        // ikkisi zavod xodimiga umuman ko'rinmaydi (izohi `WmsPermissions.PortalSelf` da).
        services.AddScoped<IAiTool, MyDebtTool>();
        services.AddScoped<IAiTool, MyTransfersTool>();
    }
}
