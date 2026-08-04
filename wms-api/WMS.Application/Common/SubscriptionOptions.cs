namespace WMS.Application.Common;

/// Bound from the "Subscription" section of appsettings (env: Subscription__TrialDays …).
public class SubscriptionOptions
{
    public const string SectionName = "Subscription";

    /// Trial length for a newly provisioned tenant when its plan does not define one.
    public int TrialDays { get; set; } = 14;

    /// Extra days after TrialEndsAt during which the tenant still works, but the client
    /// is warned. Access is blocked only after TrialEndsAt + GraceDays.
    public int GraceDays { get; set; } = 3;

    /// How long a tenant's subscription state is cached before it is re-read from the DB.
    /// Bounds how long a suspend takes to bite (default: at most one minute).
    public int StateCacheSeconds { get; set; } = 60;

    /// Warn the client when the trial ends within this many days (used by /api/subscription/me).
    public int WarnBeforeDays { get; set; } = 7;

    /// Usage percentage at which a plan limit is reported as "near limit".
    public int LimitWarnPercent { get; set; } = 80;
}
