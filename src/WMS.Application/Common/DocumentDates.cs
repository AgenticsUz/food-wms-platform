using WMS.Application.Interfaces;

namespace WMS.Application.Common;

/// <summary>
/// Hujjat sanasini yechadi va tekshiradi — kirim/chiqim uchun ham, to'lov uchun ham
/// BITTA qoida (P2.3 / P2.8).
/// </summary>
/// <remarks>
/// <para>
/// Qoida: sana berilmasa — bugun; kelajak sana HECH KIMGA ruxsat emas; o'tgan sana
/// faqat <see cref="WmsPermissions.DocumentsBackdate"/> ruxsati bilan.
/// </para>
/// <para>
/// ⚠️ Tekshiruv AYNAN shu yerda, chunki entitlement yuk tarkibiga bog'liq (atributda
/// tekshirib bo'lmaydi — u sanani ko'rmaydi). Xuddi shu naqsh
/// <c>TransferService.EnsureTransferTypeAllowedAsync</c> da ham qo'llangan.
/// </para>
/// <para>
/// ⚠️ Kun chegarasi UTC'da olinadi — butun tizimdagi kabi (<c>AnalyticsService</c> izohi).
/// Toshkent vaqti bilan kechqurun 05:00 gacha bo'lgan farq bor, lekin uni bu yerda
/// tuzatish hisobotlardagi kun chegarasi bilan ZID bo'lardi: ikkalasi bir xil qoidada
/// turishi muhimroq.
/// </para>
/// </remarks>
public static class DocumentDates
{
    /// <summary>Kelajak sana xatosi (tarjima kaliti).</summary>
    public const string FutureMessage = "Document date cannot be in the future";

    /// <summary>Orqaga sana ruxsati yo'qligi (tarjima kaliti).</summary>
    public const string BackdateForbiddenMessage = "Entering a past document date requires the '{0}' permission";

    /// <summary>Sanani yechadi: bo'sh bo'lsa — bugun; tekshiruvdan o'tmasa — xato.</summary>
    /// <param name="requested">Foydalanuvchi bergan sana (ixtiyoriy).</param>
    /// <param name="user">Joriy foydalanuvchi — orqaga sana ruxsati shundan.</param>
    /// <returns>Saqlanadigan hujjat sanasi (UTC, kun boshi).</returns>
    public static DateTime Resolve(DateTime? requested, ICurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime today = DateTime.UtcNow.Date;
        if (requested is not { } value)
        {
            return today;
        }

        DateTime date = DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        if (date > today)
        {
            throw new AppException(FutureMessage);
        }

        if (date < today && !user.HasPermission(WmsPermissions.DocumentsBackdate))
        {
            throw new ForbiddenException(BackdateForbiddenMessage, WmsPermissions.DocumentsBackdate);
        }

        return date;
    }
}
