using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

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
    private readonly IRequestWarnings _warnings;
    public ResponseLocalizationFilter(IRequestWarnings warnings) => _warnings = warnings;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: IApiResponse response }) return;

        var language = RequestLanguage.Resolve(context.HttpContext);

        if (!string.IsNullOrEmpty(response.Message))
            response.Message = Translations.Format(response.Message, language);

        // A limit warning rides along with the successful response it belongs to.
        if (_warnings.First is { } warning)
            response.Warning = new ApiWarning
            {
                Code = warning.Code,
                Message = Translations.Format(warning.Template, language, warning.Args)
            };
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
