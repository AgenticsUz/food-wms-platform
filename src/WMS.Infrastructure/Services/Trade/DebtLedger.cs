using Microsoft.EntityFrameworkCore;
using Npgsql;
using WMS.Domain.Entities;
using WMS.Infrastructure.Common;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

/// <summary>
/// Kontragentning YAGONA qarz qatorini topadi yoki yaratadi (D13).
/// </summary>
/// <remarks>
/// <para>
/// SQLite davridagi «topilmasa yarat» naqshi parallel ikki so'rovda (ikkita tasdiq, tasdiq + to'lov)
/// ikkita <c>debt</c> qatori yaratardi va balans ikkiga bo'linib ketardi. Endi
/// <c>(tenant_id, counterparty_id)</c> NOYOB: ikkinchi <c>INSERT</c> 23505 oladi — biz uni
/// yutamiz, g'olib yozgan qatorni qayta o'qiymiz va shu bilan davom etamiz (bitta qayta urinish
/// yetadi: noyoblik buzilgan bo'lsa qator bazada ALLAQACHON bor).
/// </para>
/// <para>
/// ⚠️ Ochiq tranzaksiyadan TASHQARIDA va change tracker toza paytda chaqirilsin. Postgres'da xato
/// bergan bayonot butun tranzaksiyani «aborted» qiladi — tranzaksiya ichida 23505 ni yutib bo'lmaydi.
/// Shuning uchun qator asosiy ish birligidan OLDIN alohida saqlanadi; ish birligi keyin yiqilsa ham
/// bo'sh (0) balans qoladi, bu esa «qator yo'q» bilan bir ma'noli.
/// </para>
/// <para>
/// Qaytgan qator kuzatiladi va <c>xmin</c> bilan qo'riqlanadi: uni o'zgartirgan ikki parallel
/// ish birligidan ikkinchisi <c>DbUpdateConcurrencyException</c> (409) oladi.
/// </para>
/// </remarks>
internal static class DebtLedger
{
    public static async Task<Debt> GetOrCreateAsync(WmsDbContext db, Guid counterpartyId, CancellationToken ct = default)
    {
        var debt = await db.Debts.FirstOrDefaultAsync(d => d.CounterpartyId == counterpartyId, ct);
        if (debt != null) return debt;

        debt = new Debt { CounterpartyId = counterpartyId, Amount = 0 };
        db.Debts.Add(debt);
        try
        {
            await db.SaveChangesAsync(ct);
            return debt;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Parallel so'rov bizdan oldin yaratdi — o'zimiznikini tashlab, uniknisini olamiz.
            db.Entry(debt).State = EntityState.Detached;
            return await db.Debts.FirstAsync(d => d.CounterpartyId == counterpartyId, ct);
        }
    }

    /// <summary>Postgres noyoblik buzilishi (23505) — tekshiruv <see cref="PostgresErrors"/> da.</summary>
    /// <param name="ex">Saqlash xatosi.</param>
    /// <returns>Ha — 23505.</returns>
    public static bool IsUniqueViolation(DbUpdateException ex) => PostgresErrors.IsUniqueViolation(ex);
}
