using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// <c>DateTime</c> ↔ <c>timestamptz</c>: yozishda UTC'ga keltiradi, o'qishda UTC deb belgilaydi.
/// </summary>
/// <remarks>
/// ⚠️ Npgsql <c>Kind=Unspecified</c> sanani <c>timestamptz</c> ga YOZMAYDI (istisno).
/// SQLite davrida sana matn edi va bu sezilmasdi; so'rov DTO'laridan keladigan
/// sanalar (<c>"2026-09-10"</c>) aynan Unspecified bo'lib bog'lanadi. Har servisni
/// qidirib tuzatish o'rniga bitta konvensiya: Unspecified — UTC deb qabul qilinadi
/// (wms-web sanalarni UTC sifatida yuboradi, <c>date.util.ts</c>).
/// </remarks>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => ToUtc(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
