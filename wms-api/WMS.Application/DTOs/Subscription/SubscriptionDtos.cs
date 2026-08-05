namespace WMS.Application.DTOs.Subscription;

/// Plan limits and how much of each the tenant currently uses.
/// A limit of 0 means unlimited (tenant without a plan).
public class SubscriptionLimitsDto
{
    public int MaxUsers { get; set; }
    public int CurrentUsers { get; set; }
    public int MaxWarehouses { get; set; }
    public int CurrentWarehouses { get; set; }
    public int MaxTransfersPerMonth { get; set; }
    public int CurrentTransfersThisMonth { get; set; }
}

/// <summary>
/// Everything the tenant app needs to explain its own subscription: which plan, what state,
/// how long is left, what it may use, and — when access is refused — exactly why.
/// Returned with HTTP 200 even while the tenant is blocked; that is the whole point.
/// </summary>
public class SubscriptionInfoDto
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = null!;

    public string? PlanName { get; set; }
    public string? PlanCode { get; set; }
    public decimal PlanPrice { get; set; }

    /// "Trial" | "Active" | "Suspended" — the enum name, not its number.
    public string Status { get; set; } = null!;

    public DateTime? TrialEndsAt { get; set; }
    public int? DaysUntilTrialEnd { get; set; }

    public DateTime? PaidUntil { get; set; }
    public int? DaysUntilPaidEnd { get; set; }
    public int PaymentGraceDays { get; set; }

    public bool IsBlocked { get; set; }
    /// Machine-readable reason, same vocabulary as the ApiResponse.code on a 402.
    public string? BlockedReason { get; set; }
    /// Message to show the user: the operator's own wording when they wrote one,
    /// otherwise the standard text for this reason.
    public string? BlockedMessage { get; set; }
    public DateTime? SuspendedUntil { get; set; }

    /// True when the trial or the paid period ends within WarnBeforeDays — show the banner.
    public bool IsExpiringSoon { get; set; }
    public int WarnBeforeDays { get; set; }
    public int LimitWarnPercent { get; set; }

    public SubscriptionLimitsDto Limits { get; set; } = new();

    public List<string> EnabledModules { get; set; } = new();
    public List<string> EnabledFeatures { get; set; } = new();

    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }

    /// Same shape as the login response and the public endpoint, so the client has one
    /// mapping for all three (B1).
    public DTOs.Branding.BrandingDto Branding { get; set; } = new();
}
