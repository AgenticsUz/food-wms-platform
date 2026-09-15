using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Ai;

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

        return services;
    }
}
