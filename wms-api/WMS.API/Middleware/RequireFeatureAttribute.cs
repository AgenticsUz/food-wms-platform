using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

namespace WMS.API.Middleware;

/// <summary>
/// Entitlement gate one level finer than <see cref="RequireModuleAttribute"/>: the tenant must
/// have this feature resolved to enabled (tenant override → plan → catalog default, with the
/// owning module able to veto). Answers 403 with "feature_disabled:CODE".
///
/// Several codes mean "any of". SuperAdmin bypasses. Feature state comes from the cached
/// tenant state, so this costs no extra query per request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _featureCodes;

    public RequireFeatureAttribute(params string[] featureCodes) => _featureCodes = featureCodes;

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

        if (state != null && _featureCodes.Any(code => state.EnabledFeatures.Contains(code))) return;

        var reported = _featureCodes.FirstOrDefault() ?? "unknown";
        var message = Translations.Format(Messages.FeatureDisabled,
            RequestLanguage.Resolve(context.HttpContext), string.Join(" / ", _featureCodes));

        context.Result = new ObjectResult(ApiResponse<object>.Fail(message, "feature_disabled:" + reported))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
