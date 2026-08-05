namespace WMS.Application.Common.Localization;

/// <summary>
/// Message templates that take arguments. Fixed messages need no constant — the English
/// text thrown in the code IS the lookup key — but a template must be shared between the
/// throw site and the translation table, or the two silently drift apart.
///
/// Placeholders are positional (`{0}`, `{1}`) so every language can reorder them.
/// </summary>
public static class Messages
{
    // ── Subscription refusals (customer-facing) ──
    public const string TenantMissing = "This account no longer exists. Please contact support.";
    public const string TenantInactive = "This account is deactivated. Please contact support.";
    public const string TrialExpired = "Your trial period has ended. Please choose a plan to continue.";
    public const string PaymentExpired = "Your subscription period has ended. Please contact us to renew.";

    public const string SuspendedNonPayment =
        "Your subscription is suspended because payment is overdue. Please contact us to restore access.";
    public const string SuspendedClientRequest =
        "Your account is paused at your own request. Contact us when you want it back on.";
    public const string SuspendedTechnical =
        "The system is temporarily unavailable for maintenance. Please try again later.";
    public const string SuspendedViolation = "Your account is suspended. Please contact support.";
    public const string SuspendedOther =
        "Your subscription is suspended. Please contact support to restore access.";

    // ── Entitlements ──
    public const string ModuleDisabled = "This module is not enabled for your subscription plan ({0})";
    public const string FeatureDisabled = "This feature is not enabled for your subscription ({0})";

    // ── Plan limits ──
    public const string LimitUsers = "Your plan ({0}) allows {1} users. Upgrade the plan to add more.";
    public const string LimitWarehouses = "Your plan ({0}) allows {1} warehouses. Upgrade the plan to add more.";
    public const string LimitTransfers =
        "Your plan ({0}) allows {1} transfers per month. Upgrade the plan to continue.";
    public const string LimitUsersImport = "Plan limit reached — upgrade the plan to add more users";

    // ── Custom features (S5) ──
    public const string CustomFeaturePrefix = "A custom feature code must start with '{0}'";
    public const string CustomFeatureInPlan =
        "'{0}' is a custom feature and cannot be part of a plan — grant it to the tenant instead";
    public const string UnknownFeatureCode = "Unknown feature code '{0}'";
    public const string FeatureNotFound = "Feature '{0}' not found";
}
