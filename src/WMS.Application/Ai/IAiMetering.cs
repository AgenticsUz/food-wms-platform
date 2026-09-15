namespace WMS.Application.Ai;

/// <summary>
/// AI'ning hisob-kitobi: chaqiriqdan OLDIN ruxsat, chaqiriqdan KEYIN sarf.
/// </summary>
/// <remarks>
/// <para>
/// Nega bitta interfeys: ikkisi bitta haqiqatga — <c>ai_usage</c> jadvaliga — tayanadi.
/// Ajratilsa, kvota tekshiruvi bilan uni oshiradigan yozuv boshqa-boshqa joyda turib,
/// «tekshirdim, lekin yozmadim» holati sezilmay qolardi.
/// </para>
/// <para>
/// ⚠️ Tartib SHART: <see cref="EnsureAvailableAsync"/> — provayderga chiqishdan oldin,
/// <see cref="RecordAsync"/> — javob kelgandan keyin, HATTO javob xato bo'lsa ham (token
/// baribir sarflangan bo'lishi mumkin).
/// </para>
/// </remarks>
public interface IAiMetering
{
    /// <summary>
    /// Shu tenant hozir AI'ga chiqa oladimi.
    /// </summary>
    /// <exception cref="Common.AiException">
    /// <c>ai_disabled</c> (kalit yoki feature yo'q), <c>ai_quota_exceeded</c> (oylik kvota),
    /// <c>ai_unavailable</c> (platformaning kunlik dollar shifti).
    /// </exception>
    Task EnsureAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Sarfni yozadi (tenant kunligi + platforma kunligi).</summary>
    /// <param name="model">Javob bergan model.</param>
    /// <param name="usage">Token hisobi.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Asinxron amal.</returns>
    Task RecordAsync(string model, LlmUsage usage, CancellationToken cancellationToken = default);
}
