using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "MainApi")]
public abstract class BaseController : ControllerBase
{
    protected int TenantId =>
        int.Parse(User.FindFirst("tenantId")?.Value ?? "0");

    protected int UserId =>
        int.Parse(User.FindFirst("userId")?.Value ?? "0");

    protected bool IsSuperAdmin =>
        User.FindFirst("isSuperAdmin")?.Value == "true";
}
