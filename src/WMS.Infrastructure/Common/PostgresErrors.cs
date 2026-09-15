using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WMS.Infrastructure.Common;

/// <summary>
/// Postgres xato kodlarini tanib olish — «bu xato aslida NORMAL holat» degan
/// joylar uchun yagona manba.
/// </summary>
/// <remarks>
/// Noyoblik buzilishi (23505) ikki xil ma'noda uchraydi va ikkalasi ham xato EMAS:
/// «topilmasa yarat» poygasida (<c>DebtLedger</c>) va dedup kaliti bilan navbatga
/// yozishda (<c>TelegramDigestBackgroundService</c>) — ikkinchi yozuv shunchaki
/// KERAK EMAS. Tekshiruv bir joyda turadi, chunki uni har safar qaytadan yozish
/// bitta joyda unutilishiga olib keladi: 2026-09-15 deploy'ida kunlik xulosa
/// xizmati aynan shu sababli butun tenant aylanishini uzib qo'ygan edi.
/// </remarks>
public static class PostgresErrors
{
    /// <summary>Noyoblik cheklovi buzildimi (23505).</summary>
    /// <param name="exception">EF saqlash xatosi.</param>
    /// <returns>Ha — 23505.</returns>
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
