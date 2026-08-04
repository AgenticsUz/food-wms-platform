namespace WMS.Application.Common;

/// Business-rule violation — mapped to HTTP 400 by the exception middleware.
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}

/// Requested entity does not exist (in the caller's tenant) — mapped to HTTP 404.
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
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
}

/// Entitlement refusal — the tenant's plan does not include this module.
/// Mapped to HTTP 403 with a machine-readable module code.
public class ModuleDisabledException : AppException
{
    public string ModuleCode { get; }

    public ModuleDisabledException(string moduleCode)
        : base($"This module is not enabled for your subscription plan ({moduleCode})")
        => ModuleCode = moduleCode;
}
