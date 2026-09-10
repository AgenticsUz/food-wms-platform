using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

namespace WMS.API.Middleware;

/// <inheritdoc />
/// <remarks>
/// Reads Accept-Language from the current request. The tenant app sends its selected
/// interface language explicitly, so the API answers in whatever the user is reading —
/// the browser's own preference is only the fallback.
/// </remarks>
public class RequestLanguage : IRequestLanguage
{
    private readonly IHttpContextAccessor _accessor;
    public RequestLanguage(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Current => Resolve(_accessor.HttpContext);

    /// Static so middleware and filters, which have the context in hand, can use the same rule.
    public static string Resolve(HttpContext? context)
        => Lang.FromHeader(context?.Request.Headers.AcceptLanguage.ToString());
}
