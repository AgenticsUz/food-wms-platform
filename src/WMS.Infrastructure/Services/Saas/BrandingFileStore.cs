using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
/// <remarks>
/// Files live under wwwroot and are served by the static-file middleware, without
/// authentication: a logo is not a secret, and wms-web shows it before `/api/me` resolves.
/// Nothing else is ever written to this folder.
/// <para>
/// F6: API qatlamidan Infrastructure'ga ko'chdi — <c>AddSaasModule</c> uni boshqa servislar bilan
/// birga ro'yxatdan o'tkazsin (Infrastructure <c>Microsoft.AspNetCore.App</c> ga ega). Papka nomi —
/// tenant Guid'i (Identity'dagi id), SQLite davrida <c>int</c> edi.
/// </para>
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

    public async Task<string> SaveAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var folderName = tenantId.ToString("D");
        var folder = Path.Combine(WebRoot, "uploads", "tenants", folderName);
        Directory.CreateDirectory(folder);

        var safeName = Path.GetFileName(fileName);
        await File.WriteAllBytesAsync(Path.Combine(folder, safeName), content, ct);

        return $"/{Root}/{folderName}/{safeName}";
    }

    public void Delete(string? publicUrl)
    {
        var path = ResolvePath(publicUrl);
        if (path == null) return;

        try
        {
            File.Delete(path);
        }
#pragma warning disable CA1031 // A leftover file wastes a few kilobytes; a failed request costs the user their work.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Almashtirilgan logo o'chirilmadi: {Path}", path);
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
