using WMS.Application.Common.Localization;
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

/// <param name="Message">Generic reason — an English template, translated at the edge.</param>
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
        Domain.Enums.SuspendReason.NonPayment => Messages.SuspendedNonPayment,
        Domain.Enums.SuspendReason.ClientRequest => Messages.SuspendedClientRequest,
        Domain.Enums.SuspendReason.Technical => Messages.SuspendedTechnical,
        Domain.Enums.SuspendReason.Violation => Messages.SuspendedViolation,
        _ => Messages.SuspendedOther
    };

    /// <summary>
    /// A dated suspension whose date has arrived. The operator promised the client
    /// "the system comes back on 11 August", and the client reads that as the date, not
    /// "some time within a day of it". The background job only runs every 24 hours, so
    /// relying on it alone leaves the tenant blocked for most of the day it was promised
    /// back — and that is a support call, from a client already unhappy enough to have
    /// been suspended. The job still clears the flag; this only stops the blocking early.
    /// </summary>
    public static bool IsSuspensionElapsed(TenantState tenant, DateTime utcNow)
        => tenant.SuspendedUntil is { } until && utcNow >= until;

    public static SubscriptionVerdict Evaluate(TenantState? tenant, SubscriptionOptions options, DateTime utcNow)
    {
        if (tenant == null)
            return new SubscriptionVerdict(false, TenantMissing, Messages.TenantMissing);

        if (!tenant.IsActive)
            return new SubscriptionVerdict(false, TenantInactive, Messages.TenantInactive);

        var elapsed = tenant.Status == SubscriptionStatus.Suspended && IsSuspensionElapsed(tenant, utcNow);

        if (tenant.Status == SubscriptionStatus.Suspended && !elapsed)
            return new SubscriptionVerdict(false, SuspendedCode(tenant.SuspendReason),
                SuspendedMessage(tenant.SuspendReason), tenant.SuspendPublicMessage);

        // The rest is judged as if the background job had already flipped the status back.
        var status = elapsed ? SubscriptionStatus.Active : tenant.Status;

        if (status == SubscriptionStatus.Trial && tenant.TrialEndsAt is { } ends
            && utcNow > ends.AddDays(options.GraceDays))
            return new SubscriptionVerdict(false, TrialExpired, Messages.TrialExpired);

        // Manual billing. Only bites when the tenant is actually on a plan and a paid-through
        // date exists — a tenant without either is deliberately never blocked for payment.
        // This also catches the elapsed suspension: a client who asked to be paused for two
        // months and let the payment lapse comes back blocked for non-payment, not working.
        if (status == SubscriptionStatus.Active && tenant.PlanId != null
            && tenant.PaidUntil is { } paidUntil
            && utcNow > paidUntil.AddDays(options.PaidGraceDays))
            return new SubscriptionVerdict(false, PaymentExpired, Messages.PaymentExpired);

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
