using WMS.Application.Common.Localization;

namespace WMS.Application.Common;

/// <summary>AI qatlamining mashina o'qiydigan rad kodlari — <c>ApiResponse.Code</c> ga tushadi.</summary>
/// <remarks>
/// wms-web va bot javobni AYNAN shu kodlar bo'yicha ajratadi: «o'chiq» (sotib olish taklifi),
/// «kvota tugadi» (ertaga yoki plan), «vaqtincha ishlamayapti» (keyinroq urinib ko'ring) —
/// uchtasi foydalanuvchiga butunlay boshqa narsa deyishi kerak.
/// </remarks>
public static class AiErrorCodes
{
    /// <summary>Tenantda <c>ai.chat</c> yoqilmagan yoki kalit sozlanmagan.</summary>
    public const string Disabled = "ai_disabled";

    /// <summary>Tenantning oylik so'rov kvotasi tugagan.</summary>
    public const string QuotaExceeded = "ai_quota_exceeded";

    /// <summary>Provayder javob bermadi yoki platforma kunlik shifti oshdi.</summary>
    public const string Unavailable = "ai_unavailable";

    /// <summary>Model ruxsati yo'q tool'ni chaqirdi.</summary>
    public const string ToolForbidden = "ai_tool_forbidden";
}

/// <summary>
/// AI qatlamining rad javobi. Har biri o'z HTTP holati va <see cref="Code"/> i bilan
/// (<c>ExceptionHandlingMiddleware</c> shu ikkisini javobga ko'chiradi).
/// </summary>
/// <remarks>
/// Nega bitta sinf, to'rtta emas: farq faqat holat kodi va matnida, xatti-harakat bir xil.
/// Mavjud <c>FeatureDisabledException</c> mos kelmadi — u <c>feature_disabled:KOD</c> shaklini
/// beradi, AI yuzalari esa reja bo'yicha aniq <c>ai_*</c> kodlarini kutadi.
/// </remarks>
public sealed class AiException : AppException
{
    private AiException(int status, string code, string template, params object?[] args)
        : base(template, args)
    {
        Status = status;
        Code = code;
    }

    /// <summary>HTTP holati.</summary>
    public int Status { get; }

    /// <summary>Mashina o'qiydigan kod (<see cref="AiErrorCodes"/>).</summary>
    public string Code { get; }

    /// <summary>AI bu tenantda o'chiq (feature yoki kalit) — 403.</summary>
    public static AiException Disabled() =>
        new(403, AiErrorCodes.Disabled, Messages.AiDisabled);

    /// <summary>Oylik kvota tugadi — 402 («planni ko'taring» oqimi, obuna rad javoblari bilan bir xil).</summary>
    public static AiException QuotaExceeded(int limit) =>
        new(402, AiErrorCodes.QuotaExceeded, Messages.AiQuotaExceeded, limit);

    /// <summary>Provayder yoki kunlik shift — 503, ya'ni «keyinroq urinib ko'ring».</summary>
    public static AiException Unavailable() =>
        new(503, AiErrorCodes.Unavailable, Messages.AiUnavailable);

    /// <summary>
    /// Model ruxsati yo'q tool'ni chaqirdi — 403.
    /// </summary>
    /// <remarks>
    /// Odatda YUZAGA KELMAYDI: ruxsatsiz tool modelga umuman ko'rsatilmaydi. Ko'tarilishi —
    /// registr bilan bajaruvchi ayrilgani yoki suhbat o'rtasida ruxsat olib qo'yilgani belgisi,
    /// shuning uchun bu holat jim o'tkazilmaydi.
    /// </remarks>
    public static AiException ToolForbidden(string toolCode) =>
        new(403, AiErrorCodes.ToolForbidden, Messages.AiToolForbidden, toolCode);
}
