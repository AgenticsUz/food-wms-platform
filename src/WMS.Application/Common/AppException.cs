namespace WMS.Application.Common;

/// <summary>
/// Business-rule violation — mapped to HTTP 400 by the exception middleware.
///
/// The message is written in English at the throw site and doubles as the translation key
/// (see <c>Translations</c>), so the middleware can render it in the caller's language.
/// A message with arguments must pass the template and the values separately — formatting
/// it here would destroy the key.
/// </summary>
public class AppException : Exception
{
    /// English template, e.g. "Your plan ({0}) allows {1} users."
    public string MessageTemplate { get; }
    public object?[] MessageArgs { get; }

    public AppException(string message) : base(message)
    {
        MessageTemplate = message;
        MessageArgs = [];
    }

    public AppException(string template, params object?[] args)
        : base(Safe(template, args))
    {
        MessageTemplate = template;
        MessageArgs = args ?? [];
    }

    private static string Safe(string template, object?[] args)
    {
        try { return args is { Length: > 0 } ? string.Format(template, args) : template; }
        catch (FormatException) { return template; }
    }
}

/// Requested entity does not exist (in the caller's tenant) — mapped to HTTP 404.
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string template, params object?[] args) : base(template, args) { }
}

/// Subscription-level refusal (suspended tenant, expired trial, plan limit reached) —
/// mapped to HTTP 402 Payment Required so the client can tell it apart from a validation
/// error and show the "contact us / upgrade your plan" flow.
public class PaymentRequiredException : AppException
{
    /// Machine-readable reason so the frontend can pick the right message without
    /// parsing text: "subscription_suspended", "trial_expired", "tenant_inactive",
    /// "limit_users", "limit_warehouses", "limit_transfers".
    public string Code { get; }

    public PaymentRequiredException(string code, string message) : base(message) => Code = code;

    public PaymentRequiredException(string code, string template, params object?[] args)
        : base(template, args) => Code = code;
}

/// The caller is authenticated and the target exists, but this action is not theirs to
/// take (a tenant admin reaching for a platform account). 403 rather than 404, because
/// hiding something the caller can already see in their own user list explains nothing.
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string template, params object?[] args) : base(template, args) { }
}

/// Entitlement refusal — the tenant's plan does not include this module.
/// Mapped to HTTP 403 with a machine-readable module code.
public class ModuleDisabledException : AppException
{
    public string ModuleCode { get; }

    public ModuleDisabledException(string moduleCode)
        : base(Localization.Messages.ModuleDisabled, moduleCode)
        => ModuleCode = moduleCode;
}

/// Entitlement refusal one level finer: the module is on, but this particular capability
/// is not granted to the tenant. Mapped to HTTP 403 with "feature_disabled:CODE".
/// Thrown from services where the check depends on the payload (e.g. the transfer type)
/// and therefore cannot live in an attribute.
public class FeatureDisabledException : AppException
{
    public string FeatureCode { get; }

    public FeatureDisabledException(string featureCode)
        : base(Localization.Messages.FeatureDisabled, featureCode)
        => FeatureCode = featureCode;
}
