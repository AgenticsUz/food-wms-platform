using WMS.Application.DTOs.Branding;

namespace WMS.Application.Interfaces;

/// Tenant branding: upload/remove logos, set the colour and read the identity back.
/// <remarks>
/// F6 (D14): yozuv FAQAT Console'ning WMS bo'limidan (<c>/admin/v1/tenants/{id}/branding|logo</c>) —
/// tenant admini o'z o'rnatmasini qayta brendlay olmaydi (SQLite davridagidek). O'qish wms-web
/// uchun <c>/api/me</c> da. Anonim <c>GetPublicAsync</c> (login sahifasi) O'CHDI — login Identity'da.
/// </remarks>
public interface IBrandingService
{
    Task<BrandingDto> GetAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>#RRGGBB; <c>null</c>/bo'sh satr — standart mavzuga qaytish.</summary>
    Task<BrandingDto> SetColorAsync(Guid tenantId, string? brandColor, CancellationToken ct = default);

    Task<BrandingDto> UploadLogoAsync(Guid tenantId, LogoKind kind, LogoUpload upload, CancellationToken ct = default);
    Task<BrandingDto> RemoveLogoAsync(Guid tenantId, LogoKind kind, CancellationToken ct = default);
}

/// <summary>
/// Where logo files live (<c>wwwroot/uploads/tenants/{tenantId}/</c>, compose'da <c>uploads</c> hajmi).
/// </summary>
/// <remarks>
/// PDF va Excel generatorlari ham shuni oladi (<c>ResolvePath</c>) — ularga URL emas, bayt kerak.
/// </remarks>
public interface IBrandingFileStore
{
    /// Writes the file and returns the public URL ("/uploads/tenants/{guid}/logo-wide-a3f9.png").
    Task<string> SaveAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default);

    /// Deletes a previously stored file by its public URL. Missing files are not an error.
    void Delete(string? publicUrl);

    /// Absolute path of a stored file, or null when it does not exist — used by the PDF and
    /// Excel generators, which need bytes rather than a URL.
    string? ResolvePath(string? publicUrl);
}
