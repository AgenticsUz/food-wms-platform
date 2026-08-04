using WMS.Domain.Enums;

namespace WMS.Application.Common;

/// Snapshot of everything needed to decide whether a tenant may use the API right now,
/// plus what it is entitled to. Cached for a short window by <c>ITenantStateService</c>.
public class TenantState
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public bool IsActive { get; init; }
    public SubscriptionStatus Status { get; init; }
    public DateTime? TrialEndsAt { get; init; }
    public int? PlanId { get; init; }

    /// Manual billing: paid through this date. Null = payment is never enforced.
    public DateTime? PaidUntil { get; init; }

    public SuspendReason? SuspendReason { get; init; }
    public string? SuspendPublicMessage { get; init; }
    public DateTime? SuspendedUntil { get; init; }

    public HashSet<string> EnabledModules { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    /// Resolved feature set: tenant override → plan → catalog default, with modules winning.
    public HashSet<string> EnabledFeatures { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <param name="Message">Generic reason text.</param>
/// <param name="PublicMessage">Operator-written message for this tenant, when set — the
/// client shows it instead of <paramref name="Message"/>.</param>
public record SubscriptionVerdict(bool Allowed, string? Code, string? Message, string? PublicMessage = null)
{
    public static readonly SubscriptionVerdict Ok = new(true, null, null);
}

/// <summary>
/// Single place that decides "may this tenant work?". Used by the login flow, the per-request
/// middleware and the tenant-facing subscription endpoint, so they can never drift apart.
/// </summary>
public static class SubscriptionPolicy
{
    public const string TenantMissing = "tenant_missing";
    public const string TenantInactive = "tenant_inactive";
    public const string TrialExpired = "trial_expired";
    public const string PaymentExpired = "payment_expired";

    public static string SuspendedCode(SuspendReason? reason) => reason switch
    {
        Domain.Enums.SuspendReason.NonPayment => "suspended_nonpayment",
        Domain.Enums.SuspendReason.ClientRequest => "suspended_request",
        Domain.Enums.SuspendReason.Technical => "suspended_technical",
        Domain.Enums.SuspendReason.Violation => "suspended_violation",
        _ => "suspended_other"
    };

    private static string SuspendedMessage(SuspendReason? reason) => reason switch
    {
        Domain.Enums.SuspendReason.NonPayment =>
            "Your subscription is suspended because payment is overdue. Please contact us to restore access.",
        Domain.Enums.SuspendReason.ClientRequest =>
            "Your account is paused at your own request. Contact us when you want it back on.",
        Domain.Enums.SuspendReason.Technical =>
            "The system is temporarily unavailable for maintenance. Please try again later.",
        Domain.Enums.SuspendReason.Violation =>
            "Your account is suspended. Please contact support.",
        _ => "Your subscription is suspended. Please contact support to restore access."
    };

    public static SubscriptionVerdict Evaluate(TenantState? tenant, SubscriptionOptions options, DateTime utcNow)
    {
        if (tenant == null)
            return new SubscriptionVerdict(false, TenantMissing,
                "This account no longer exists. Please contact support.");

        if (!tenant.IsActive)
            return new SubscriptionVerdict(false, TenantInactive,
                "This account is deactivated. Please contact support.");

        if (tenant.Status == SubscriptionStatus.Suspended)
            return new SubscriptionVerdict(false, SuspendedCode(tenant.SuspendReason),
                SuspendedMessage(tenant.SuspendReason), tenant.SuspendPublicMessage);

        if (tenant.Status == SubscriptionStatus.Trial && tenant.TrialEndsAt is { } ends
            && utcNow > ends.AddDays(options.GraceDays))
            return new SubscriptionVerdict(false, TrialExpired,
                "Your trial period has ended. Please choose a plan to continue.");

        // Manual billing. Only bites when the tenant is actually on a plan and a paid-through
        // date exists — a tenant without either is deliberately never blocked for payment.
        if (tenant.Status == SubscriptionStatus.Active && tenant.PlanId != null
            && tenant.PaidUntil is { } paidUntil
            && utcNow > paidUntil.AddDays(options.PaidGraceDays))
            return new SubscriptionVerdict(false, PaymentExpired,
                "Your subscription period has ended. Please contact us to renew.");

        return SubscriptionVerdict.Ok;
    }

    /// Days left until the trial ends (negative once inside the grace period).
    /// Null when the tenant is not on a dated trial.
    public static int? TrialDaysLeft(TenantState tenant, DateTime utcNow)
        => tenant.Status == SubscriptionStatus.Trial && tenant.TrialEndsAt is { } ends
            ? (int)Math.Ceiling((ends - utcNow).TotalDays)
            : null;

    /// Days left in the paid period (negative once inside the payment grace period).
    public static int? PaidDaysLeft(DateTime? paidUntil, DateTime utcNow)
        => paidUntil is { } until ? (int)Math.Ceiling((until - utcNow).TotalDays) : null;
}
