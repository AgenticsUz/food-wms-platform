using System.Globalization;
using System.Text.Json;
using WMS.Application.Ai;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>
/// Argumentli tool'ning umumiy asosi: sxema, o'qish va hajm chegarasi.
/// </summary>
/// <typeparam name="TArgs">Argument DTO'si — sxema SHUNDAN chiqariladi.</typeparam>
/// <remarks>
/// <para>
/// Nega asos sinf: har tool argumentni o'zi o'qiganda, sxemani buzgan javobga munosabat
/// ham har joyda boshqacha bo'lardi — biri istisno ko'taradi, ikkinchisi sukut qiymat
/// bilan davom etadi. Bu yerda qoida bitta: noto'g'ri argument — XATO NATIJA, model uni
/// o'qib o'zini tuzatadi.
/// </para>
/// <para>
/// ⚠️ Sxema TUR bo'yicha bir marta hisoblanadi (<see cref="CachedSchema"/>): generic
/// sinfning statik maydoni har yopiq tur uchun alohida. Har chaqiriqda qayta eksport
/// qilish ham sekin, ham keraksiz — sxema ish vaqtida o'zgarmaydi.
/// </para>
/// </remarks>
public abstract class AiTool<TArgs> : IAiTool
    where TArgs : new()
{
    private static readonly JsonElement CachedSchema = AiToolSchema.For<TArgs>();

    /// <inheritdoc />
    public abstract string Code { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract string PermissionCode { get; }

    /// <inheritdoc />
    public virtual string? FeatureCode => null;

    /// <inheritdoc />
    public JsonElement Schema => CachedSchema;

    /// <inheritdoc />
    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments, AiToolContext context, CancellationToken cancellationToken = default)
    {
        if (!AiToolSchema.TryRead(arguments, out TArgs args, out string? error))
        {
            return AiToolResult.Fail($"Argumentlar sxemaga mos kelmadi: {error}");
        }

        return await RunAsync(args, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Tool mantig'i — argumentlar allaqachon o'qilgan.</summary>
    /// <param name="args">O'qilgan argumentlar.</param>
    /// <param name="context">So'rov konteksti.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Natija.</returns>
    protected abstract Task<AiToolResult> RunAsync(
        TArgs args, AiToolContext context, CancellationToken cancellationToken);

    /// <summary>Ko'pi bilan nechta qator modelga ketadi.</summary>
    /// <remarks>
    /// ⚠️ Chegara SHART: 500 qatorli qoldiq ro'yxati kontekstni to'ldirib, javob sifatini
    /// tushiradi va har chaqiriqni qimmatlashtiradi. Kesib yuborish ham yaramaydi — model
    /// qolganini KO'RMAGANINI bilmaydi va «hammasi shu» deb javob berardi. Shuning uchun
    /// ro'yxat kesilganda matn buni OSHKORA aytadi.
    /// </remarks>
    protected const int MaxRows = 50;

    /// <summary>Ro'yxatni chegaraga qisqartiradi va kesilganini aytadi.</summary>
    /// <typeparam name="T">Qator turi.</typeparam>
    /// <param name="rows">To'liq ro'yxat.</param>
    /// <param name="truncated">Ro'yxat kesildimi.</param>
    /// <returns>Chegaradagi ro'yxat.</returns>
    protected static IReadOnlyList<T> Limit<T>(IReadOnlyList<T> rows, out bool truncated)
    {
        truncated = rows.Count > MaxRows;
        return truncated ? [.. rows.Take(MaxRows)] : rows;
    }

    /// <summary>Miqdorni matnga: ortiqcha nollar tashlanadi («12,000» emas, «12»).</summary>
    /// <param name="value">Miqdor.</param>
    /// <returns>Matn.</returns>
    protected static string Quantity(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Pul summasi — uch xonali guruh bilan («1 250 000»).</summary>
    /// <param name="value">Summa.</param>
    /// <returns>Matn.</returns>
    protected static string Money(decimal value) =>
        Math.Round(value, 2).ToString("#,##0.##", CultureInfo.InvariantCulture).Replace(",", " ", StringComparison.Ordinal);

    /// <summary>Sana — ISO shaklida (model va odam uchun bir xil o'qiladi).</summary>
    /// <param name="value">Sana.</param>
    /// <returns>Matn.</returns>
    protected static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

/// <summary>Argumentsiz tool (<c>today_summary</c>, <c>finance_summary</c>).</summary>
/// <remarks>Bo'sh sxema ham <c>type: object</c> bo'lishi kerak — provayder shart qiladi.</remarks>
public sealed class NoArgs;
