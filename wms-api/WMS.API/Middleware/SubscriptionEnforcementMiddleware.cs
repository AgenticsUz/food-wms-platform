using System.Text.Json;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;

namespace WMS.API.Middleware;

/// <summary>
/// Enforces the subscription on EVERY authenticated request, not just at login.
/// Without this a suspended tenant keeps working until its 7-day JWT expires.
///
/// Blocked cases (see <see cref="SubscriptionPolicy"/>): tenant deleted, deactivated,
/// suspended, or a trial past TrialEndsAt + grace. Answers 402 with a machine-readable
/// code so the client can show the right screen.
///
/// Exempt: unauthenticated requests, SuperAdmin (control plane), auth endpoints,
/// /api/subscription/* (the blocked client must still be able to see WHY), health, swagger.
/// </summary>
public class SubscriptionEnforcementMiddleware
{
    private static readonly string[] ExemptPrefixes =
    [
        "/api/auth", "/api/subscription", "/api/admin", "/health", "/swagger"
    ];

    private readonly RequestDelegate _next;
    private readonly SubscriptionOptions _options;

    public SubscriptionEnforcementMiddleware(RequestDelegate next, IOptions<SubscriptionOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ITenantStateService tenantState)
    {
        if (!ShouldCheck(context))
        {
            await _next(context);
            return;
        }

        if (!int.TryParse(context.User.FindFirst("tenantId")?.Value, out var tenantId) || tenantId <= 0)
        {
            await _next(context);
            return;
        }

        var state = await tenantState.GetAsync(tenantId, context.RequestAborted);
        var verdict = SubscriptionPolicy.Evaluate(state, _options, DateTime.UtcNow);
        if (verdict.Allowed)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(
            ApiResponse<object>.Fail(verdict.Message!, verdict.Code!),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static bool ShouldCheck(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true) return false;
        if (context.User.FindFirst("isSuperAdmin")?.Value == "true") return false;

        var path = context.Request.Path.Value ?? "";
        return !ExemptPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
