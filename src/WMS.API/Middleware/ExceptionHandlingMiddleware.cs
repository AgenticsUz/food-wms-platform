using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Surfaces;
using WMS.Application.Common;
using WMS.Application.Common.Localization;

namespace WMS.API.Middleware;

/// <summary>
/// Istisno → javob. Ikki yuza, ikki shakl (D15): <c>/api</c> — WMS'ning <c>ApiResponse</c> + <c>code</c>
/// (wms-web 402/403 kodlarini shu shaklda o'qiydi), <c>/admin/v1</c> — RFC 9457 ProblemDetails
/// (Console har mahsulotdan shu shaklni kutadi).
/// </summary>
/// <remarks>
/// Matn ingliz tilidagi shablon — u tarjima kaliti ham; tarjima shu yerda, chaqiruvchining tili
/// ma'lum bo'lgan yagona nuqtada. 500 da ichki tafsilot faqat logga yoziladi.
/// </remarks>
public sealed class ExceptionHandlingMiddleware
{
    public const string ConcurrencyConflictMessage = "The record was changed by someone else. Reload and try again.";
    public const string MalformedBodyMessage = "The request body is malformed";

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
#pragma warning disable CA1031 // Oxirgi to'r: har qanday istisno javobga aylanadi.
        catch (Exception ex) when (!context.Response.HasStarted && ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            string language = RequestLanguage.Resolve(context);

            (int status, string template, object?[] args, string? code) = ex switch
            {
                NotFoundException e => (StatusCodes.Status404NotFound, e.MessageTemplate, e.MessageArgs, (string?)null),
                PaymentRequiredException e => (StatusCodes.Status402PaymentRequired, e.MessageTemplate, e.MessageArgs, e.Code),
                ModuleDisabledException e => (StatusCodes.Status403Forbidden, e.MessageTemplate, e.MessageArgs, "module_disabled:" + e.ModuleCode),
                FeatureDisabledException e => (StatusCodes.Status403Forbidden, e.MessageTemplate, e.MessageArgs, "feature_disabled:" + e.FeatureCode),
                ForbiddenException e => (StatusCodes.Status403Forbidden, e.MessageTemplate, e.MessageArgs, null),

                // AI rad javoblari o'z holatini O'ZI olib yuradi (402/403/503): «o'chiq»,
                // «kvota tugadi» va «vaqtincha ishlamayapti» mijozga butunlay boshqa narsa deydi.
                AiException e => (e.Status, e.MessageTemplate, e.MessageArgs, e.Code),
                AppException e => (StatusCodes.Status400BadRequest, e.MessageTemplate, e.MessageArgs, null),

                // D13: `xmin` ushlagan parallel yozuv. 409 — «qayta yuklab takrorlang», 500 emas.
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, ConcurrencyConflictMessage, [], "concurrency_conflict"),

                // Buzuq tana mijozning xatosi — 500 bo'lsa monitoring server nosozligi deb chalg'irdi (F5 da o'lchangan).
                BadHttpRequestException or JsonException => (StatusCodes.Status400BadRequest, MalformedBodyMessage, [], null),

                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", [], null),
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Kutilmagan istisno: {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            string message = Translations.Format(template, language, args);
            context.Response.Clear();
            context.Response.StatusCode = status;

            if (WmsSurfaces.IsAdminPath(context.Request.Path))
            {
                ProblemDetails problem = new()
                {
                    Status = status,
                    Title = ReasonTitle(status),
                    Detail = message,
                    Type = "https://wms.agentics.uz/problems/" + status.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Instance = context.Request.Path.Value,
                };
                problem.Extensions["correlationId"] = context.TraceIdentifier;
                if (code is not null)
                {
                    // `errorCode` — Console'ning platforma klienti AYNAN shu kalitni o'qiydi (Wash/HRM
                    // shakli); usiz WMS biznes kodlari Console'ga yetmasdi (F6, W2·4 da o'lchangan).
                    // `code` — wms-web va eski shakl bilan mos qolishi uchun.
                    problem.Extensions["errorCode"] = code;
                    problem.Extensions["code"] = code;
                }

                await context.Response.WriteAsJsonAsync(problem, WmsJson.Options, "application/problem+json");
                return;
            }

            ApiResponse<object> payload = code is null ? ApiResponse<object>.Fail(message) : ApiResponse<object>.Fail(message, code);
            await context.Response.WriteAsJsonAsync(payload, WmsJson.Options);
        }
    }

    private static string ReasonTitle(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Validation",
        StatusCodes.Status402PaymentRequired => "PaymentRequired",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "NotFound",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Unexpected",
    };
}
