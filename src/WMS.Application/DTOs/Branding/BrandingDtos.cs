namespace WMS.Application.DTOs.Branding;

/// <summary>
/// A tenant's visual identity. Deliberately three fields and no more: branding must stay
/// DATA, so one build serves every customer. "Move the sidebar", "reorder the menu" and
/// "colour this module differently" are code, not branding, and are refused as such.
/// </summary>
/// <remarks>
/// F6 (D14): uni Console'ning WMS bo'limi yozadi, wms-web esa <c>/api/me</c> dan o'qiydi.
/// SQLite davridagi <c>PublicBrandingDto</c> (anonim login sahifasi) O'CHDI — login sahifasi
/// Identity'da va u ataylab neytral (<c>LoginPageNeutralTextTests</c>).
/// </remarks>
public class BrandingDto
{
    /// Wide logo — expanded sidebar, report headers.
    public string? LogoUrl { get; set; }
    /// Square logo — collapsed sidebar, favicon.
    public string? LogoSquareUrl { get; set; }
    /// One primary colour as #RRGGBB; the client derives the rest of the palette from it.
    public string? BrandColor { get; set; }
}

/// <summary><c>PUT /admin/v1/tenants/{id}/branding</c>. <c>null</c> yoki bo'sh satr — standart mavzuga qaytish.</summary>
public class UpdateBrandingDto
{
    public string? BrandColor { get; set; }
}

/// One uploaded logo, described without any web-layer types so the Application layer
/// stays free of IFormFile.
public record LogoUpload(Stream Content, string FileName, string? ContentType, long Length);

public enum LogoKind
{
    Wide = 1,
    Square = 2
}
