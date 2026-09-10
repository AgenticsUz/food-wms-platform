using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Platform.Infrastructure.Tenancy;
using WMS.API.Surfaces;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.API.Middleware;

/// <summary>
/// Har muvaffaqiyatli mutatsiyani (POST/PUT/PATCH/DELETE) <c>wms.audit_log</c> ga yozadi: kim, nima,
/// qaysi yozuv, qachon. Jurnal so'rovni hech qachon buzmaydi.
/// </summary>
/// <remarks>
/// F6: bajaruvchi Identity <c>sub</c> bilan (Console operatorining WMS profili yo'q), tenant —
/// joriy kontekstdan. Console amallari (<c>/admin/v1</c>) Console TANLAGAN tenantga yoziladi:
/// mijoz o'z jurnalida kim uni to'xtatganini ko'rsin. Tenant kontekstsiz amal yozilmaydi — RLS
/// tenantsiz qatorni baribir qabul qilmasdi.
/// </remarks>
public sealed class AuditLogFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE", "PATCH" };

    private readonly ILogger<AuditLogFilter> _logger;

    public AuditLogFilter(ILogger<AuditLogFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ActionExecutedContext executed = await next();

        HttpContext http = context.HttpContext;
        if (!MutatingMethods.Contains(http.Request.Method)
            || (executed.Exception is not null && !executed.ExceptionHandled)
            || http.Response.StatusCode >= 400)
        {
            return;
        }

        try
        {
            if (http.RequestServices.GetRequiredService<ICurrentTenant>().TenantId is null)
            {
                return;
            }

            ICurrentUser user = http.RequestServices.GetRequiredService<ICurrentUser>();
            ControllerActionDescriptor? action = context.ActionDescriptor as ControllerActionDescriptor;

            WmsDbContext db = http.RequestServices.GetRequiredService<WmsDbContext>();
            db.AuditLogs.Add(new AuditLog
            {
                ActorSub = user.Sub,
                UserName = user.FullName,
                Action = http.Request.Method.ToUpperInvariant(),
                EntityType = action?.ControllerName ?? string.Empty,
                EntityAction = action?.ActionName,
                EntityId = context.RouteData.Values.TryGetValue("id", out object? id) ? id?.ToString() : null,
                Path = http.Request.Path.Value ?? string.Empty,
                StatusCode = http.Response.StatusCode,
                IsPlatformAction = WmsSurfaces.IsAdminPath(http.Request.Path),
                CorrelationId = http.TraceIdentifier,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(http.RequestAborted);
        }
#pragma warning disable CA1031 // Jurnal yozuvi so'rovni hech qachon buzmasligi kerak.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Audit yozuvi yozilmadi");
        }
    }
}
