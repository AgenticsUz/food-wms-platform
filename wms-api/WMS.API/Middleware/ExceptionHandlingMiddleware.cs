using System.Text.Json;
using WMS.Application.Common;
using WMS.Application.Common.Localization;

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
            // The thrown text is the English template; it is translated here, at the only
            // point where the caller's language is known.
            var language = RequestLanguage.Resolve(context);
            var text = ex is AppException app
                ? Translations.Format(app.MessageTemplate, language, app.MessageArgs)
                : Translations.Format("An unexpected error occurred", language);

            var (statusCode, message, code) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, text, null),
                PaymentRequiredException pre => (StatusCodes.Status402PaymentRequired, text, pre.Code),
                ModuleDisabledException mde => (StatusCodes.Status403Forbidden, text, "module_disabled:" + mde.ModuleCode),
                FeatureDisabledException fde => (StatusCodes.Status403Forbidden, text, "feature_disabled:" + fde.FeatureCode),
                ForbiddenException => (StatusCodes.Status403Forbidden, text, null),
                AppException => (StatusCodes.Status400BadRequest, text, null),
                _ => (StatusCodes.Status500InternalServerError, text, (string?)null)
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
