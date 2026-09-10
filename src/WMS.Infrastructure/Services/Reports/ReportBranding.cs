using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>
/// Hujjat (PDF, Excel) uchun brendlash: bosiladigan nom, asosiy rang va logo BAYT sifatida —
/// PDF va Excel URL emas, piksel talab qiladi.
/// </summary>
/// <remarks>
/// <para>
/// Hammasi yumshoq tushadi: logo yo'q — nom bosiladi, nom yo'q — platforma nomi, fayl
/// o'qilmasa — hech narsa. Hisobot bezak sababli HECH QACHON yiqilmasligi kerak.
/// </para>
/// <para>
/// F6: <c>tenantId</c> parametri o'chdi (D4) — tenant <see cref="WmsDbContext.CurrentTenantId"/>
/// dan (so'rov yoki fon vazifa scope'ining konteksti). Parametr qolsa chaqiruvchi begona
/// tenant logosini o'z hujjatiga bosishi mumkin bo'lardi. Namespace eski
/// (<c>WMS.Infrastructure.Services</c>) — transfer va yuk xati PDF'lari uni o'zgarishsiz chaqiradi.
/// </para>
/// </remarks>
public class ReportBranding
{
    public const string DefaultName = "WMS Platform";

    public string Name { get; init; } = DefaultName;
    public string? Color { get; init; }
    public byte[]? LogoBytes { get; init; }

    /// <summary>Joriy tenantning brendlashi; tenant konteksti yo'q yoki xato — standart brendlash.</summary>
    public static async Task<ReportBranding> LoadAsync(WmsDbContext db, IBrandingFileStore files,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(files);

        if (db.CurrentTenantId is not { } tenantId) return new ReportBranding();

        try
        {
            // `tenant` — platforma jadvali (RLS yo'q), shuning uchun id bo'yicha oshkora qidiruv.
            var tenant = await db.Tenants.AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Select(t => new { t.Name, t.Code, t.LogoUrl, t.BrandColor })
                .FirstOrDefaultAsync(ct);

            if (tenant == null) return new ReportBranding();

            byte[]? bytes = null;
            var path = files.ResolvePath(tenant.LogoUrl);

            // SVG ataylab o'tkazib yuboriladi: PDF dvigateli rastr chizadi va chizilmagan logo
            // butun hujjatni o'zi bilan olib ketardi.
            if (path != null && !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                bytes = await File.ReadAllBytesAsync(path, ct);

            // JIT nusxasida Name birinchi yozuvda KOD bilan to'ldiriladi (Tenant izohi) —
            // haqiqiy nomni Console qo'ymaguncha hujjatda texnik kod chiqmasin.
            var hasRealName = !string.IsNullOrWhiteSpace(tenant.Name)
                && !string.Equals(tenant.Name, tenant.Code, StringComparison.Ordinal);

            return new ReportBranding
            {
                Name = hasRealName ? tenant.Name : DefaultName,
                Color = tenant.BrandColor,
                LogoBytes = bytes
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ReportBranding();
        }
    }
}
