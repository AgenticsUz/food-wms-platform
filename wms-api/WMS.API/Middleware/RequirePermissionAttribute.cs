using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Infrastructure.Persistence;

namespace WMS.API.Middleware;

/// Enforces the RolePermission system: the authenticated user must hold a role that
/// grants the given permission code (seeded in WmsDbContext, e.g. "settings.users").
/// Class-level usage guards every action; add a second attribute on mutations for
/// the stricter "*.manage" codes — both must then be granted.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;
    public RequirePermissionAttribute(string permission) => _permission = permission;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            return;

        if (!int.TryParse(context.HttpContext.User.FindFirst("userId")?.Value, out var userId) || userId <= 0)
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("Unauthorized"));
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<WmsDbContext>();
        var hasPermission = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .AnyAsync(rp => rp.Permission.Code == _permission);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("Permission denied"))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
