using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Application.Common;
using WMS.Application.Interfaces;

namespace WMS.API.Middleware;

/// <summary>
/// Entitlement gate: the caller's tenant must have at least one of the given modules
/// enabled (TenantModule.IsEnabled), otherwise the endpoint answers 403 with the
/// "module_disabled:CODE" error code.
///
/// This is the backend half of plan gating — the Angular moduleGuard only hides the UI,
/// it does not stop a direct API call. Module state is read through ITenantStateService,
/// which caches it for a short window, so this adds no per-request DB query.
///
/// Several codes mean "any of" (e.g. Counterparties needs SUPPLIERS or CLIENTS).
/// SuperAdmin bypasses the check — the control plane must reach every tenant.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireModuleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _moduleCodes;

    public RequireModuleAttribute(params string[] moduleCodes) => _moduleCodes = moduleCodes;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            return;

        var user = context.HttpContext.User;
        if (user.FindFirst("isSuperAdmin")?.Value == "true") return;

        if (!int.TryParse(user.FindFirst("tenantId")?.Value, out var tenantId) || tenantId <= 0)
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("Unauthorized"));
            return;
        }

        var tenantState = context.HttpContext.RequestServices.GetRequiredService<ITenantStateService>();
        var state = await tenantState.GetAsync(tenantId, context.HttpContext.RequestAborted);

        var enabled = state != null && _moduleCodes.Any(code => state.EnabledModules.Contains(code));
        if (enabled) return;

        var reported = _moduleCodes.FirstOrDefault() ?? "UNKNOWN";
        context.Result = new ObjectResult(ApiResponse<object>.Fail(
            $"This module is not enabled for your subscription plan ({string.Join(" / ", _moduleCodes)})",
            "module_disabled:" + reported))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
