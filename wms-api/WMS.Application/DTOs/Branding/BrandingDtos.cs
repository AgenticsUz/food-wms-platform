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

    /// <summary>
    /// Qo'llab-quvvatlash aloqasi. Login sahifasi obuna bloklanganini aynan shu yerda
    /// ko'rsatadi, lekin o'sha paytda tokeni yo'q — `/api/subscription/me` ga kira olmaydi.
    /// Shuning uchun bu qiymatlar tokensiz javobga ham qo'shiladi: aks holda frontend
    /// ularni o'zida saqlashga majbur bo'ladi va raqamni almashtirish uchun butun
    /// Angular ilovasini qayta yig'ish kerak bo'lardi.
    /// </summary>
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
}

/// One uploaded logo, described without any web-layer types so the Application layer
/// stays free of IFormFile.
public record LogoUpload(Stream Content, string FileName, string? ContentType, long Length);

public enum LogoKind
{
    Wide = 1,
    Square = 2
}
