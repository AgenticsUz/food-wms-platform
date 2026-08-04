using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMS.Application.Common;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

/// <summary>
/// The public front door now that self-service registration is closed: a visitor asks for
/// a demo, and the platform owner calls them back and creates the tenant by hand.
///
/// Anonymous and therefore rate limited per IP. A repeat submission from the same phone
/// within a day returns 200 without creating another row — the visitor sees success, we
/// do not accumulate duplicates.
/// </summary>
[ApiController]
[Route("api/leads")]
[AllowAnonymous]
[EnableRateLimiting("leads")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leads;
    public LeadsController(ILeadService leads) => _leads = leads;

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateLeadDto dto)
        => Ok(ApiResponse<LeadDto>.Ok(await _leads.SubmitAsync(dto, LeadSource.Website),
            "Thank you — we will contact you shortly"));
}
