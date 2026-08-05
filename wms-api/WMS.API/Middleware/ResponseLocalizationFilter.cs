using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Application.Common;
using WMS.Application.Common.Localization;

namespace WMS.API.Middleware;

/// <summary>
/// Translates the short confirmations controllers put in <c>ApiResponse.Message</c>
/// ("Deleted", "Payment recorded", …) into the caller's language.
///
/// It sits in a filter rather than in each controller so a new endpoint is localized by
/// default: write the English message, add one row to <c>Translations</c>, done. Errors take
/// the other path (the exception middleware), and the machine-readable <c>Code</c> is never
/// touched — only humans read Message.
/// </summary>
public class ResponseLocalizationFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: IApiResponse response }) return;
        if (string.IsNullOrEmpty(response.Message)) return;

        response.Message = Translations.Format(response.Message, RequestLanguage.Resolve(context.HttpContext));
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
