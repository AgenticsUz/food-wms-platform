using System.Text.Json;
using WMS.Application.Common;

namespace WMS.API.Middleware;

/// Maps uncaught exceptions to ApiResponse.Fail payloads:
/// NotFoundException → 404, AppException → 400, anything else → 500 with a generic
/// message (internal details are logged, never returned to the client).
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, message, code) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message, null),
                PaymentRequiredException pre => (StatusCodes.Status402PaymentRequired, ex.Message, pre.Code),
                ModuleDisabledException mde => (StatusCodes.Status403Forbidden, ex.Message, "module_disabled:" + mde.ModuleCode),
                AppException => (StatusCodes.Status400BadRequest, ex.Message, null),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", (string?)null)
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted) throw;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var payload = code == null
                ? ApiResponse<object>.Fail(message)
                : ApiResponse<object>.Fail(message, code);
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
