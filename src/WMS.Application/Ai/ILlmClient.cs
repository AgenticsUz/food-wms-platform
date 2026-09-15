namespace WMS.Application.Ai;

/// <summary>
/// LLM provayderi — WMS kodining model bilan gaplashadigan YAGONA seam'i.
/// </summary>
/// <remarks>
/// <para>
/// Nega interfeys: Anthropic SDK turlarini (F10 §0.9) faqat bitta sinf biladi
/// (<c>AnthropicLlmClient</c>). Gateway, tool registri va testlar shu shartnomani ko'radi,
/// ya'ni SDK versiyasi yangilanganda yoki testda javob fixture'dan olinganda qolgan kod
/// o'zgarmaydi.
/// </para>
/// <para>
/// ⚠️ Realizatsiya qayta urinishni O'ZI qilmaydi: sikl va byudjet gateway'da
/// (<c>AiGateway</c>, A1) — u yerda nechta aylanish bo'lgani va qancha token sarflangani
/// ma'lum. Bu yerda ko'r-ko'rona retry xarajatni jimgina ikki barobar qilardi.
/// </para>
/// </remarks>
public interface ILlmClient
{
    /// <summary>
    /// Kalit sozlanganmi. <see langword="false"/> — AI modul o'chiq, <see cref="CompleteAsync"/>
    /// chaqirilmasligi kerak.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>Konfiguratsiyadagi model (<c>Ai:Model</c>).</summary>
    string Model { get; }

    /// <summary>Bitta chaqiriq: so'rov → javob (tool chaqiriqlari bilan).</summary>
    /// <exception cref="Common.AiException">
    /// Provayder javob bermadi yoki kalit sozlanmagan — <c>ai_unavailable</c> / <c>ai_disabled</c>.
    /// </exception>
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
