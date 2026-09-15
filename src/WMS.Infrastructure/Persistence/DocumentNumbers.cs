using Microsoft.EntityFrameworkCore;
using Npgsql;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class DocumentNumbers : IDocumentNumbers
{
    /// <summary>
    /// Bitta gapda: qator bo'lmasa yaratadi, bo'lsa oshiradi va yangi qiymatni qaytaradi.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Nega xom SQL, nega EF emas.</b> Raqam hujjat SAQLANAYOTGAN paytda kerak bo'ladi,
    /// ya'ni chaqiruvchining kuzatuvida hali yozilmagan obyektlar turadi. EF orqali
    /// hisoblagichni saqlash <c>SaveChanges</c> ni chaqirardi va O'SHA yarim tayyor
    /// obyektlarni ham bazaga yuborib yuborardi. Xom SQL kuzatuvga umuman tegmaydi.
    /// </para>
    /// <para>
    /// ⚠️ <c>ON CONFLICT</c> ning arbitri — qisman noyob indeks, shuning uchun predikat
    /// (<c>is_deleted = false</c>) gapda AYNAN takrorlanadi; aks holda Postgres indeksni
    /// tanimaydi va «no unique or exclusion constraint matching» beradi.
    /// </para>
    /// <para>
    /// Tenant RLS bilan qo'yiladi: <c>current_setting('app.tenant_id')</c> — interceptor har
    /// ulanishda o'rnatadigan qiymat. Kontekst bo'lmasa gap <c>INSERT</c> da yiqiladi
    /// (fail-closed), jimgina boshqa tenantga yozib ketmaydi.
    /// </para>
    /// </remarks>
    private const string NextSql = """
        INSERT INTO wms.tenant_counter (id, tenant_id, kind, value, created_at, updated_at, is_deleted)
        VALUES (@id, NULLIF(current_setting('app.tenant_id', true), '')::uuid, @kind, 1, now(), now(), false)
        ON CONFLICT (tenant_id, kind) WHERE is_deleted = false
        DO UPDATE SET value = wms.tenant_counter.value + 1, updated_at = now()
        RETURNING value AS "Value";
        """;

    private readonly WmsDbContext _db;

    /// <summary>Kontekstni oladi.</summary>
    /// <param name="db">Baza konteksti.</param>
    public DocumentNumbers(WmsDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<int> NextAsync(string kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        NpgsqlParameter[] parameters =
        [
            new("id", Guid.CreateVersion7()),
            new("kind", kind),
        ];

        // ⚠️ `ToListAsync`, `SingleAsync` EMAS: `SingleAsync` xom SQL ustiga o'z so'rovini
        // QURADI (`LIMIT` bilan), `INSERT … RETURNING` esa kompozitsiyaga yaramaydi va EF
        // «non-composable SQL» xatosini beradi. Gap bitta qator qaytaradi.
        List<int> rows = await _db.Database.SqlQueryRaw<int>(NextSql, parameters).ToListAsync(cancellationToken);

        return rows.Count == 1
            ? rows[0]
            : throw new InvalidOperationException($"Hisoblagich '{kind}' uchun raqam qaytmadi.");
    }

    /// <summary>Hisoblagichni mavjud eng katta raqamgacha ko'taradi (seed va backfill uchun).</summary>
    /// <param name="kind">Hisoblagich turi (<see cref="TenantCounter.Kinds"/>).</param>
    /// <param name="value">Eng katta berilgan raqam.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Asinxron amal.</returns>
    /// <remarks>
    /// Hisoblagich faqat OSHADI: allaqachon kattaroq qiymatda bo'lsa tegilmaydi — aks holda
    /// seed eski holatga qaytarib, keyingi hujjat mavjud raqamni takrorlardi.
    /// </remarks>
    public async Task EnsureAtLeastAsync(string kind, int value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        if (value <= 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO wms.tenant_counter (id, tenant_id, kind, value, created_at, updated_at, is_deleted)
            VALUES (@id, NULLIF(current_setting('app.tenant_id', true), '')::uuid, @kind, @value, now(), now(), false)
            ON CONFLICT (tenant_id, kind) WHERE is_deleted = false
            DO UPDATE SET value = GREATEST(wms.tenant_counter.value, EXCLUDED.value), updated_at = now();
            """;

        NpgsqlParameter[] parameters =
        [
            new("id", Guid.CreateVersion7()),
            new("kind", kind),
            new("value", value),
        ];

        await _db.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }
}
