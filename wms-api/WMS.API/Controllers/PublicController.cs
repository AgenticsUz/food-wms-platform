using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMS.Application.Common;
using WMS.Application.DTOs.Branding;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

/// <summary>
/// The only endpoints that answer without a token. Today the login page runs on one shared
/// URL, so nobody knows whose branding to show and the client does not call this yet — but
/// the moment tenants get their own subdomain it works without a backend change.
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
[EnableRateLimiting("public")]
public class PublicController : ControllerBase
{
    private readonly IBrandingService _branding;
    public PublicController(IBrandingService branding) => _branding = branding;

    /// Always 200, even for a slug that does not exist: a 404 here would let anyone
    /// enumerate which companies are on the platform.
    [HttpGet("branding")]
    public async Task<IActionResult> GetBranding([FromQuery] string? slug, CancellationToken ct)
        => Ok(ApiResponse<PublicBrandingDto>.Ok(await _branding.GetPublicAsync(slug, ct)));
}
