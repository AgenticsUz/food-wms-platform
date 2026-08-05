using WMS.Application.DTOs.Branding;

namespace WMS.Application.Interfaces;

/// Tenant branding: upload/remove logos and read the identity back.
/// SuperAdmin-only for writes — a tenant admin cannot rebrand its own installation.
public interface IBrandingService
{
    Task<BrandingDto> GetAsync(int tenantId, CancellationToken ct = default);

    /// Anonymous login-page lookup. An unknown slug returns empty branding with HTTP 200:
    /// answering 404 would turn the endpoint into a slug oracle.
    Task<PublicBrandingDto> GetPublicAsync(string? slug, CancellationToken ct = default);

    Task<BrandingDto> UploadLogoAsync(int tenantId, LogoKind kind, LogoUpload upload, CancellationToken ct = default);
    Task<BrandingDto> RemoveLogoAsync(int tenantId, LogoKind kind, CancellationToken ct = default);
}

/// <summary>
/// Where logo files live. Implemented in the API layer, which is the only place that knows
/// about wwwroot and how a file becomes a URL.
/// </summary>
public interface IBrandingFileStore
{
    /// Writes the file and returns the public URL ("/uploads/tenants/3/logo-wide-a3f9.png").
    Task<string> SaveAsync(int tenantId, string fileName, byte[] content, CancellationToken ct = default);

    /// Deletes a previously stored file by its public URL. Missing files are not an error.
    void Delete(string? publicUrl);

    /// Absolute path of a stored file, or null when it does not exist — used by the PDF and
    /// Excel generators, which need bytes rather than a URL.
    string? ResolvePath(string? publicUrl);
}
