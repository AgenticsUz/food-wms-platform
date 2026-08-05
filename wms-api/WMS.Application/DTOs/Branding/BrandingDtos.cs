namespace WMS.Application.DTOs.Branding;

/// <summary>
/// A tenant's visual identity. Deliberately three fields and no more: branding must stay
/// DATA, so one build serves every customer. "Move the sidebar", "reorder the menu" and
/// "colour this module differently" are code, not branding, and are refused as such.
/// </summary>
public class BrandingDto
{
    /// Wide logo — expanded sidebar, login page, report headers.
    public string? LogoUrl { get; set; }
    /// Square logo — collapsed sidebar, favicon.
    public string? LogoSquareUrl { get; set; }
    /// One primary colour as #RRGGBB; the client derives the rest of the palette from it.
    public string? BrandColor { get; set; }

    public static readonly BrandingDto Empty = new();
}

/// What the anonymous login-page endpoint returns. Nothing but the visual identity —
/// no counts, no status, nothing that would confirm whether a slug exists.
public class PublicBrandingDto
{
    public string? Name { get; set; }
    public BrandingDto Branding { get; set; } = new();
}

/// One uploaded logo, described without any web-layer types so the Application layer
/// stays free of IFormFile.
public record LogoUpload(Stream Content, string FileName, string? ContentType, long Length);

public enum LogoKind
{
    Wide = 1,
    Square = 2
}
