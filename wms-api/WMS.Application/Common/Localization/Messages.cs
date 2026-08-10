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

    // ── Branding (B1) ──
    public const string InvalidBrandColor = "Brand colour must be a hex value like #2E7D32";
    public const string LogoEmpty = "The uploaded file is empty";
    public const string LogoTooLarge = "The logo must be {0} KB or smaller";
    public const string LogoFormat = "The logo must be an SVG, PNG or WebP image";
    public const string LogoUnsafeSvg =
        "This SVG contains scripts or external references and cannot be used as a logo";
    public const string LogoTooBig = "The logo may be at most {0}×{1} px (this one is {2}×{3})";

    // ── Limit warnings (B2) — not errors, the operation succeeded ──
    public const string LimitWarnUsers = "You are close to your plan limit: {0} of {1} users";
    public const string LimitWarnWarehouses = "You are close to your plan limit: {0} of {1} warehouses";
    public const string LimitWarnTransfers = "You are close to your plan limit: {0} of {1} transfers this month";

    /// Rolga tarifga kirmaydigan ruxsat biriktirildi (B5). Saqlanadi, lekin ishlamaydi —
    /// admin buni bilib tursin.
    public const string PermissionsOutsidePlan =
        "{0} of the selected permissions are outside your plan and will not take effect until it is extended";

    // ── Password reset ──
    public const string PasswordTooShort = "The password must be at least {0} characters";
    public const string CannotResetPlatformUser = "This is a platform account — only a platform administrator can reset it";
    public const string UseChangePasswordInstead = "Use \"change password\" for your own account";

    // ── Custom features (S5) ──
    public const string CustomFeaturePrefix = "A custom feature code must start with '{0}'";
    public const string CustomFeatureInPlan =
        "'{0}' is a custom feature and cannot be part of a plan — grant it to the tenant instead";
    public const string UnknownFeatureCode = "Unknown feature code '{0}'";
    public const string FeatureNotFound = "Feature '{0}' not found";
}
