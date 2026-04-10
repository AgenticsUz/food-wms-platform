using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected int TenantId =>
        int.Parse(User.FindFirst("tenantId")?.Value ?? "0");

    protected int UserId =>
        int.Parse(User.FindFirst("userId")?.Value ?? "0");
}
