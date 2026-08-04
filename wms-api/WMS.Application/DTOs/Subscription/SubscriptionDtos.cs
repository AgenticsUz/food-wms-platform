using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Subscription;

/// One plan limit and how much of it the tenant currently uses.
/// Limit = 0 means unlimited (tenant without a plan).
public class LimitUsageDto
{
    public string Key { get; set; } = null!;   // "users" | "warehouses" | "transfersThisMonth"
    public int Limit { get; set; }
    public int Used { get; set; }
    public bool IsUnlimited => Limit <= 0;
    public int Percent => Limit <= 0 ? 0 : (int)Math.Round(Used * 100.0 / Limit);
    public bool IsExceeded => Limit > 0 && Used >= Limit;
}

/// Everything the tenant app needs to show its own subscription state:
/// which plan, what status, how many trial days remain, and current limit usage.
public class SubscriptionInfoDto
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public int? PlanId { get; set; }
    public string? PlanName { get; set; }
    public string? PlanCode { get; set; }
    public decimal PlanPrice { get; set; }

    public SubscriptionStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int? TrialDaysLeft { get; set; }
    public int GraceDays { get; set; }
    /// True when the trial ends within SubscriptionOptions.WarnBeforeDays — show the banner.
    public bool IsExpiringSoon { get; set; }
    /// True when access is currently blocked (suspended / trial past grace).
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }

    public List<string> EnabledModules { get; set; } = new();
    public List<LimitUsageDto> Limits { get; set; } = new();
    public int LimitWarnPercent { get; set; }
}
