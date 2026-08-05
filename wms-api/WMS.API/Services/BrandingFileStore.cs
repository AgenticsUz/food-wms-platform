using WMS.Application.Interfaces;

namespace WMS.API.Services;

/// <inheritdoc />
/// <remarks>
/// Files live under wwwroot and are served by the static-file middleware, without
/// authentication: a logo is not a secret, and the login page has to show it before anyone
/// has signed in. Nothing else is ever written to this folder.
/// </remarks>
public class BrandingFileStore : IBrandingFileStore
{
    private const string Root = "uploads/tenants";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BrandingFileStore> _logger;

    public BrandingFileStore(IWebHostEnvironment env, ILogger<BrandingFileStore> logger)
    {
        _env = env;
        _logger = logger;
    }

    private string WebRoot => _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

    public async Task<string> SaveAsync(int tenantId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var folder = Path.Combine(WebRoot, "uploads", "tenants", tenantId.ToString());
        Directory.CreateDirectory(folder);

        var safeName = Path.GetFileName(fileName);
        await File.WriteAllBytesAsync(Path.Combine(folder, safeName), content, ct);

        return $"/{Root}/{tenantId}/{safeName}";
    }

    public void Delete(string? publicUrl)
    {
        var path = ResolvePath(publicUrl);
        if (path == null) return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            // A leftover file wastes a few kilobytes; a failed request costs the user their work.
            _logger.LogWarning(ex, "Could not delete replaced logo {Path}", path);
        }
    }

    public string? ResolvePath(string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl)) return null;
        if (!publicUrl.StartsWith($"/{Root}/", StringComparison.Ordinal)) return null;

        var relative = publicUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(WebRoot, relative));

        // Keep the path inside wwwroot even if the stored URL was tampered with.
        var root = Path.GetFullPath(WebRoot);
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;

        return File.Exists(path) ? path : null;
    }
}
