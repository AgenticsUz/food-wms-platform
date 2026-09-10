using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Branding;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
/// <remarks>
/// <c>tenant</c> — platforma jadvali (RLS yo'q), shuning uchun brendlash tenant kontekstisiz ham
/// yoziladi; o'zgarish <c>/api/me</c> ga keshdan o'tib yetsin deb har yozuvdan keyin holat bekor qilinadi.
/// </remarks>
public partial class BrandingService : IBrandingService
{
    /// 512 KB. A logo that needs more than this is a scanned photograph, and every user
    /// would download it on every page load.
    private const long MaxBytes = 512 * 1024;
    private const int WideMaxWidth = 600, WideMaxHeight = 200;
    private const int SquareMaxSide = 512;

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();

    private readonly WmsDbContext _db;
    private readonly IBrandingFileStore _files;
    private readonly ITenantStateService _tenantState;

    public BrandingService(WmsDbContext db, IBrandingFileStore files, ITenantStateService tenantState)
    {
        _db = db;
        _files = files;
        _tenantState = tenantState;
    }

    /// Validates a brand colour. Contrast is the client's problem; the format is ours.
    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;

        var value = color.Trim();
        if (!HexColor().IsMatch(value))
            throw new AppException(Messages.InvalidBrandColor);
        return value.ToUpperInvariant();
    }

    public async Task<BrandingDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");
        return ToDto(tenant);
    }

    public async Task<BrandingDto> SetColorAsync(Guid tenantId, string? brandColor, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");

        // Empty clears it back to the default theme.
        tenant.BrandColor = NormalizeColor(brandColor);
        await _db.SaveChangesAsync(ct);
        _tenantState.Invalidate(tenantId);
        return ToDto(tenant);
    }

    public async Task<BrandingDto> UploadLogoAsync(Guid tenantId, LogoKind kind, LogoUpload upload,
        CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");

        if (upload.Length <= 0) throw new AppException(Messages.LogoEmpty);
        if (upload.Length > MaxBytes)
            throw new AppException(Messages.LogoTooLarge, MaxBytes / 1024);

        var content = await ReadAllAsync(upload.Content, MaxBytes, ct);
        if (content.Length == 0) throw new AppException(Messages.LogoEmpty);
        var extension = ValidateFormat(content, upload);
        ValidateDimensions(content, kind);

        // The hash in the file name is what makes a replaced logo actually appear: browsers
        // cache /uploads/... aggressively, and a stable name would keep showing the old one.
        var hash = Convert.ToHexString(SHA256.HashData(content))[..8].ToLowerInvariant();
        var fileName = $"logo-{(kind == LogoKind.Wide ? "wide" : "square")}-{hash}{extension}";

        var previous = kind == LogoKind.Wide ? tenant.LogoUrl : tenant.LogoSquareUrl;
        var url = await _files.SaveAsync(tenantId, fileName, content, ct);

        if (kind == LogoKind.Wide) tenant.LogoUrl = url; else tenant.LogoSquareUrl = url;
        await _db.SaveChangesAsync(ct);

        // Only after the new one is safely recorded — a crash in between must not leave the
        // tenant with no logo at all.
        if (!string.Equals(previous, url, StringComparison.Ordinal)) _files.Delete(previous);
        _tenantState.Invalidate(tenantId);

        return ToDto(tenant);
    }

    public async Task<BrandingDto> RemoveLogoAsync(Guid tenantId, LogoKind kind, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");

        var previous = kind == LogoKind.Wide ? tenant.LogoUrl : tenant.LogoSquareUrl;
        if (kind == LogoKind.Wide) tenant.LogoUrl = null; else tenant.LogoSquareUrl = null;

        await _db.SaveChangesAsync(ct);
        _files.Delete(previous);
        _tenantState.Invalidate(tenantId);

        return ToDto(tenant);
    }

    private static BrandingDto ToDto(Tenant t) => new()
    {
        LogoUrl = t.LogoUrl, LogoSquareUrl = t.LogoSquareUrl, BrandColor = t.BrandColor
    };

    /// Returns the file extension to store under, after checking the bytes — not the
    /// declared content type, which the uploader controls.
    private static string ValidateFormat(byte[] content, LogoUpload upload)
    {
        var declared = (upload.ContentType ?? "").ToLowerInvariant();

        if (declared.StartsWith(ImageInspector.Svg, StringComparison.Ordinal) || ImageInspector.LooksLikeSvg(content))
        {
            if (!ImageInspector.LooksLikeSvg(content)) throw new AppException(Messages.LogoFormat);
            if (ImageInspector.IsUnsafeSvg(content)) throw new AppException(Messages.LogoUnsafeSvg);
            return ".svg";
        }

        var size = ImageInspector.GetPixelSize(content);
        if (size == null) throw new AppException(Messages.LogoFormat);

        // GetPixelSize only understands PNG and WebP, so a readable header proves the format.
        return content[1] == 0x50 ? ".png" : ".webp";
    }

    private static void ValidateDimensions(byte[] content, LogoKind kind)
    {
        var size = ImageInspector.GetPixelSize(content);
        if (size == null) return;   // SVG scales — no pixel limit applies

        var (width, height) = size.Value;
        if (kind == LogoKind.Wide && (width > WideMaxWidth || height > WideMaxHeight))
            throw new AppException(Messages.LogoTooBig, WideMaxWidth, WideMaxHeight, width, height);
        if (kind == LogoKind.Square && (width > SquareMaxSide || height > SquareMaxSide))
            throw new AppException(Messages.LogoTooBig, SquareMaxSide, SquareMaxSide, width, height);
    }

    /// Reads at most limit+1 bytes: a lying Content-Length must not let an upload fill the disk.
    private static async Task<byte[]> ReadAllAsync(Stream stream, long limit, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > limit) throw new AppException(Messages.LogoTooLarge, limit / 1024);
        }
        return buffer.ToArray();
    }
}
