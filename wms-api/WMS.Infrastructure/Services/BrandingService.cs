using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Branding;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class BrandingService : IBrandingService
{
    /// 512 KB. A logo that needs more than this is a scanned photograph, and every user
    /// would download it on every page load.
    private const long MaxBytes = 512 * 1024;
    private const int WideMaxWidth = 600, WideMaxHeight = 200;
    private const int SquareMaxSide = 512;

    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly WmsDbContext _db;
    private readonly IBrandingFileStore _files;
    private readonly ITenantStateService _tenantState;
    private readonly IConfiguration _config;

    public BrandingService(WmsDbContext db, IBrandingFileStore files, ITenantStateService tenantState,
        IConfiguration config)
    {
        _db = db;
        _files = files;
        _tenantState = tenantState;
        _config = config;
    }

    /// Validates a brand colour. Contrast is the client's problem; the format is ours.
    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;

        var value = color.Trim();
        if (!HexColor.IsMatch(value))
            throw new AppException(Messages.InvalidBrandColor);
        return value.ToUpperInvariant();
    }

    public async Task<BrandingDto> GetAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new BrandingDto
            {
                LogoUrl = t.LogoUrl, LogoSquareUrl = t.LogoSquareUrl, BrandColor = t.BrandColor
            })
            .FirstOrDefaultAsync(ct);

        return tenant ?? BrandingDto.Empty;
    }

    public async Task<PublicBrandingDto> GetPublicAsync(string? slug, CancellationToken ct = default)
    {
        // Aloqa ma'lumoti slugdan qat'i nazar qaytadi — noto'g'ri slug yozgan mijoz ham
        // kimga qo'ng'iroq qilishni bilishi kerak.
        var support = (Phone: _config["Support:Phone"], Email: _config["Support:Email"]);

        var normalized = (slug ?? "").Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return new PublicBrandingDto { SupportPhone = support.Phone, SupportEmail = support.Email };

        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Slug == normalized && t.IsActive)
            .Select(t => new PublicBrandingDto
            {
                Name = t.Name,
                Branding = new BrandingDto
                {
                    LogoUrl = t.LogoUrl, LogoSquareUrl = t.LogoSquareUrl, BrandColor = t.BrandColor
                }
            })
            .FirstOrDefaultAsync(ct);

        // Unknown slug → empty branding, still 200. Anything else would let anyone probe
        // which companies use the platform.
        tenant ??= new PublicBrandingDto();
        tenant.SupportPhone = support.Phone;
        tenant.SupportEmail = support.Email;
        return tenant;
    }

    public async Task<BrandingDto> UploadLogoAsync(int tenantId, LogoKind kind, LogoUpload upload,
        CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");

        if (upload.Length <= 0) throw new AppException(Messages.LogoEmpty);
        if (upload.Length > MaxBytes)
            throw new AppException(Messages.LogoTooLarge, MaxBytes / 1024);

        var content = await ReadAllAsync(upload.Content, MaxBytes, ct);
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

    public async Task<BrandingDto> RemoveLogoAsync(int tenantId, LogoKind kind, CancellationToken ct = default)
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

    private static BrandingDto ToDto(Domain.Entities.Tenant t) => new()
    {
        LogoUrl = t.LogoUrl, LogoSquareUrl = t.LogoSquareUrl, BrandColor = t.BrandColor
    };

    /// Returns the file extension to store under, after checking the bytes — not the
    /// declared content type, which the uploader controls.
    private static string ValidateFormat(byte[] content, LogoUpload upload)
    {
        var declared = (upload.ContentType ?? "").ToLowerInvariant();

        if (declared.StartsWith(ImageInspector.Svg) || ImageInspector.LooksLikeSvg(content))
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
