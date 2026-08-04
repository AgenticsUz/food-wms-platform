using WMS.Domain.Enums;

namespace WMS.Application.Common;

/// Snapshot of everything needed to decide whether a tenant may use the API right now.
/// Cached for a short window by <c>ITenantStateService</c>.
public class TenantState
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public bool IsActive { get; init; }
    public SubscriptionStatus Status { get; init; }
    public DateTime? TrialEndsAt { get; init; }
    public int? PlanId { get; init; }
    public HashSet<string> EnabledModules { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public record SubscriptionVerdict(bool Allowed, string? Code, string? Message)
{
    public static readonly SubscriptionVerdict Ok = new(true, null, null);
}

/// Single place that decides "may this tenant work?". Used by both the login flow
/// (AuthService) and the per-request middleware, so the two can never drift apart.
public static class SubscriptionPolicy
{
    public const string TenantMissing = "tenant_missing";
    public const string TenantInactive = "tenant_inactive";
    public const string Suspended = "subscription_suspended";
    public const string TrialExpired = "trial_expired";

    public static SubscriptionVerdict Evaluate(TenantState? tenant, SubscriptionOptions options, DateTime utcNow)
    {
        if (tenant == null)
            return new SubscriptionVerdict(false, TenantMissing,
                "This account no longer exists. Please contact support.");

        if (!tenant.IsActive)
            return new SubscriptionVerdict(false, TenantInactive,
                "This account is deactivated. Please contact support.");

        if (tenant.Status == SubscriptionStatus.Suspended)
            return new SubscriptionVerdict(false, Suspended,
                "Your subscription is suspended. Please contact support to restore access.");

        if (tenant.Status == SubscriptionStatus.Trial && tenant.TrialEndsAt is { } ends
            && utcNow > ends.AddDays(options.GraceDays))
            return new SubscriptionVerdict(false, TrialExpired,
                "Your trial period has ended. Please choose a plan to continue.");

        return SubscriptionVerdict.Ok;
    }

    /// Days left until the trial ends (negative once inside the grace period).
    /// Null when the tenant is not on a dated trial.
    public static int? TrialDaysLeft(TenantState tenant, DateTime utcNow)
        => tenant.Status == SubscriptionStatus.Trial && tenant.TrialEndsAt is { } ends
            ? (int)Math.Ceiling((ends - utcNow).TotalDays)
            : null;
}
