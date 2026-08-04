using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.API.Middleware;

/// <summary>
/// Har muvaffaqiyatli o'zgartiruvchi so'rovni (POST/PUT/DELETE/PATCH) AuditLog'ga yozadi:
/// kim (userId/name), qaysi amal (metod + action), qaysi entity (controller + id), qachon.
/// Logging hech qachon so'rovni buzmaydi (try/catch bilan o'raladi).
/// </summary>
public class AuditLogFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE", "PATCH" };

    private readonly ILogger<AuditLogFilter> _logger;
    public AuditLogFilter(ILogger<AuditLogFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        try
        {
            var http = context.HttpContext;
            var method = http.Request.Method;
            if (!MutatingMethods.Contains(method)) return;

            // Faqat autentifikatsiya qilingan asosiy foydalanuvchi amallari (login/anonim emas)
            if (!int.TryParse(http.User.FindFirst("tenantId")?.Value, out var tenantId) || tenantId <= 0)
                return;

            // Amal muvaffaqiyatli tugagan bo'lsa (exception yo'q va 2xx/3xx status)
            if (executed.Exception != null && !executed.ExceptionHandled) return;
            var status = http.Response.StatusCode;
            if (status >= 400) return;

            int.TryParse(http.User.FindFirst("userId")?.Value, out var userId);
            var userName = http.User.FindFirst("fullName")?.Value;

            string controller = "", action = "";
            if (context.ActionDescriptor is ControllerActionDescriptor cad)
            {
                controller = cad.ControllerName;
                action = cad.ActionName;
            }

            int? entityId = null;
            if (context.RouteData.Values.TryGetValue("id", out var idVal)
                && int.TryParse(idVal?.ToString(), out var parsedId))
                entityId = parsedId;

            // Platforma amallari (`/api/admin/*`) MAQSAD tenantga yoziladi, superadminning
            // o'z tenantiga emas — aks holda mijoz kim uni suspend qilganini ko'ra olmaydi.
            var path = http.Request.Path.Value ?? "";
            var isPlatformAction = path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);
            var actorTenantId = tenantId;
            if (isPlatformAction
                && path.StartsWith("/api/admin/tenants", StringComparison.OrdinalIgnoreCase)
                && entityId is > 0)
                tenantId = entityId.Value;

            var db = http.RequestServices.GetRequiredService<WmsDbContext>();
            db.AuditLogs.Add(new AuditLog
            {
                TenantId = tenantId,
                IsPlatformAction = isPlatformAction,
                ActorTenantId = isPlatformAction ? actorTenantId : null,
                UserId = userId > 0 ? userId : null,
                UserName = userName,
                Action = method.ToUpperInvariant(),
                EntityType = controller,
                EntityAction = action,
                EntityId = entityId,
                Path = http.Request.Path.Value ?? "",
                StatusCode = status,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit yozuvi so'rovni hech qachon buzmasligi kerak
            _logger.LogWarning(ex, "Audit log yozib bo'lmadi");
        }
    }
}
