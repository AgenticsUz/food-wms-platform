using System.Text;
using System.Text.RegularExpressions;

namespace WMS.Application.Common;

/// <summary>
/// Reads image dimensions straight from the file header and screens SVG content.
///
/// Written by hand rather than pulled from a library because we need exactly two things
/// (how big is it, is it safe to serve) for three formats, and an image library is a large
/// dependency plus a steady stream of CVEs for a feature that uploads a logo.
/// </summary>
public static class ImageInspector
{
    public const string Png = "image/png";
    public const string Webp = "image/webp";
    public const string Svg = "image/svg+xml";

    /// Pixel size of a raster image. Null for SVG (it scales) or an unreadable header.
    public static (int Width, int Height)? GetPixelSize(byte[] content)
        => ReadPng(content) ?? ReadWebp(content);

    /// PNG: 8-byte signature, then the IHDR chunk whose first two big-endian ints are the size.
    private static (int, int)? ReadPng(byte[] b)
    {
        if (b.Length < 24) return null;
        if (!(b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)) return null;
        if (Encoding.ASCII.GetString(b, 12, 4) != "IHDR") return null;

        var width = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        var height = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        return (width, height);
    }

    /// WebP: "RIFF"…"WEBP", then a VP8 / VP8L / VP8X chunk, each storing the size differently.
    private static (int, int)? ReadWebp(byte[] b)
    {
        if (b.Length < 30) return null;
        if (Encoding.ASCII.GetString(b, 0, 4) != "RIFF" || Encoding.ASCII.GetString(b, 8, 4) != "WEBP")
            return null;

        var chunk = Encoding.ASCII.GetString(b, 12, 4);
        switch (chunk)
        {
            case "VP8 ":
                // Lossy: 14-bit width/height after the 3-byte start code at offset 23.
                if (b.Length < 30) return null;
                return (((b[27] << 8) | b[26]) & 0x3FFF, ((b[29] << 8) | b[28]) & 0x3FFF);

            case "VP8L":
                // Lossless: 14 bits each, packed across four bytes from offset 21.
                if (b.Length < 25) return null;
                var bits = b[21] | (b[22] << 8) | (b[23] << 16) | (b[24] << 24);
                return ((bits & 0x3FFF) + 1, ((bits >> 14) & 0x3FFF) + 1);

            case "VP8X":
                // Extended: 24-bit canvas size minus one, from offset 24.
                if (b.Length < 30) return null;
                var w = (b[24] | (b[25] << 8) | (b[26] << 16)) + 1;
                var h = (b[27] | (b[28] << 8) | (b[29] << 16)) + 1;
                return (w, h);

            default:
                return null;
        }
    }

    private static readonly Regex Dangerous = new(
        @"<\s*script|<\s*foreignObject|\son\w+\s*=|javascript:|<\s*iframe|<\s*use[^>]*href\s*=\s*[""']https?:|xlink:href\s*=\s*[""']https?:|<\s*image[^>]*href\s*=\s*[""']https?:|@import|<!ENTITY",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// True when an SVG contains anything that executes or reaches off-site. Logos are served
    /// from our own origin, so a hostile SVG would run in the customer's session — we reject
    /// rather than sanitize, because a stripped-down SVG that still renders wrong is worse
    /// than a clear "use a PNG".
    /// </summary>
    public static bool IsUnsafeSvg(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content);
        return Dangerous.IsMatch(text);
    }

    public static bool LooksLikeSvg(byte[] content)
    {
        var head = Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, 512));
        return head.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }
}
