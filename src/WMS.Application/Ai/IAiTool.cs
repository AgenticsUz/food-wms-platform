using System.Text.Json;
using WMS.Domain.Enums;

namespace WMS.Application.Ai;

/// <summary>
/// Bitta so'rovning AI konteksti — tool'lar SHU yozuv ostida bajariladi.
/// </summary>
/// <param name="Channel">Qaysi yuzadan (javob shakli shunga qarab tanlanadi).</param>
/// <param name="UserProfileId">Kim so'ramoqda (<c>user_profile.id</c>); bot kontekstida ham to'la.</param>
/// <param name="Permissions">Amaldagi ruxsatlar — registr SHU to'plamga qarab filtrlanadi.</param>
/// <param name="EnabledFeatures">Tenantda yoqilgan feature kodlari.</param>
/// <param name="Language">Suhbat tili (<c>uz</c>/<c>ru</c>).</param>
/// <remarks>
/// ⚠️ Ruxsatlar shu yerda OSHKORA uzatiladi, <c>ICurrentUser</c> dan olinmaydi: Telegram
/// yangilanishi HTTP so'rovi emas va u yerda «joriy foydalanuvchi» yo'q. Bitta gateway
/// ikkala kanalga xizmat qilishi kerak, ya'ni huquqlar kanalga bog'liq bo'lmagan joyda
/// turishi shart.
/// </remarks>
public sealed record AiToolContext(
    AiChannel Channel,
    Guid? UserProfileId,
    IReadOnlySet<string> Permissions,
    IReadOnlySet<string> EnabledFeatures,
    string Language)
{
    public bool HasPermission(string code) => Permissions.Contains(code);

    public bool HasFeature(string? code) => code is null || EnabledFeatures.Contains(code);
}

/// <summary>
/// Tool natijasi: modelga matn, yuzaga strukturali ma'lumot.
/// </summary>
/// <param name="Text">Modelga (va Telegramga) ketadigan qisqa matn.</param>
/// <param name="Data">Web komponenti chizadigan DTO; <see langword="null"/> — chizadigan narsa yo'q.</param>
/// <param name="IsError">Tool xato qaytardimi — provayderga shu bayroq bilan uzatiladi.</param>
/// <remarks>
/// Ikkala shakl ham kerak: model faqat matnni o'qiydi, web esa jadval chizadi. Bitta shaklda
/// qoldirilsa yo Telegram JSON ko'rsatardi, yo web matndan jadval qayta yasashga urinardi.
/// </remarks>
public sealed record AiToolResult(string Text, object? Data = null, bool IsError = false)
{
    public static AiToolResult Fail(string text) => new(text, null, IsError: true);
}

/// <summary>
/// Modelga beriladigan bitta amal.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Xavfsizlik chegarasi — SHU interfeys, prompt emas</b> (F10 §0.3). Ruxsati yo'q tool
/// modelga umuman KO'RSATILMAYDI: model mavjudligini bilmagan amalni chaqira olmaydi, ya'ni
/// prompt injection uni «ko'ndirolmaydi».
/// </para>
/// <para>
/// ⚠️ Tool biznes-logika YOZMAYDI (§0.4): u mavjud servisni chaqiradi va natijani shakllaydi.
/// Aks holda bir xil qoida ikki joyda — UI'da va AI'da — ayri yashab ketardi.
/// </para>
/// </remarks>
public interface IAiTool
{
    /// <summary>Model ko'radigan nom (<c>stock_query</c>) — suhbat tarixida ham shu yoziladi.</summary>
    string Code { get; }

    /// <summary>Model uchun tavsif: QACHON chaqirilishini aytadi, nima qilishini emas.</summary>
    string Description { get; }

    /// <summary>Argument sxemasi (JSON Schema, <c>type: object</c>).</summary>
    JsonElement Schema { get; }

    /// <summary>Shu tool uchun SHART bo'lgan ruxsat (<c>WmsPermissions</c>).</summary>
    string PermissionCode { get; }

    /// <summary>Tenant feature'i (<c>FeatureCodes</c>); <see langword="null"/> — feature talab qilmaydi.</summary>
    string? FeatureCode { get; }

    /// <summary>Bajaradi. Argumentlar sxemaga MOS kelmasligi mumkin — tool o'zi tekshiradi.</summary>
    Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiToolContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Foydalanuvchiga RUXSAT ETILGAN tool'lar ro'yxati.
/// </summary>
public interface IAiToolRegistry
{
    /// <summary>Kontekstga ko'ra filtrlangan to'plam — modelga AYNAN shu ro'yxat beriladi.</summary>
    IReadOnlyList<IAiTool> Available(AiToolContext context);

    /// <summary>
    /// Model chaqirgan tool'ni topadi.
    /// </summary>
    /// <exception cref="Common.AiException">
    /// Tool yo'q yoki bu foydalanuvchiga ruxsat etilmagan — <c>ai_tool_forbidden</c>.
    /// </exception>
    /// <remarks>
    /// ⚠️ Ro'yxatdan filtrlash YETARLI EMAS: model o'zi o'ylab topgan nomni ham yuborishi
    /// mumkin, shuning uchun bajarishdan oldin tekshiruv QAYTA qilinadi.
    /// </remarks>
    IAiTool Require(string code, AiToolContext context);
}
