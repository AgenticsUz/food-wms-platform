using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <summary>
/// The branding a generated document needs: the name to print, the accent colour, and the
/// logo as bytes (PDF and Excel need pixels, not a URL).
///
/// Everything here degrades: no logo prints the name, no name prints the platform name, an
/// unreadable file prints nothing. A report must never fail because of decoration.
/// </summary>
public class ReportBranding
{
    public string Name { get; init; } = "WMS Platform";
    public string? Color { get; init; }
    public byte[]? LogoBytes { get; init; }

    public static async Task<ReportBranding> LoadAsync(WmsDbContext db, IBrandingFileStore files,
        int tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await db.Tenants.AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Select(t => new { t.Name, t.LogoUrl, t.BrandColor })
                .FirstOrDefaultAsync(ct);

            if (tenant == null) return new ReportBranding();

            byte[]? bytes = null;
            var path = files.ResolvePath(tenant.LogoUrl);

            // SVG is skipped on purpose: the PDF engine draws raster images, and a logo that
            // fails to render would take the whole document with it.
            if (path != null && !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                bytes = await File.ReadAllBytesAsync(path, ct);

            return new ReportBranding
            {
                Name = string.IsNullOrWhiteSpace(tenant.Name) ? "WMS Platform" : tenant.Name,
                Color = tenant.BrandColor,
                LogoBytes = bytes
            };
        }
        catch
        {
            return new ReportBranding();
        }
    }
}
