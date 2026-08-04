using System.Text.Json.Serialization;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Platform;

// Platform enums cross the wire as NAMES ("BankTransfer", "ClientRequest", "Won"), which is
// what the agreed contract shows and what both consoles send. Tenant-facing enums stay
// numeric — changing those would break every existing screen.

// ── Manual billing (S1) ──────────────────────────────────────────────────────

public class RecordPaymentDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlatformPaymentMethod Method { get; set; } = PlatformPaymentMethod.BankTransfer;
    public string? Note { get; set; }
}

public class PaymentRecordDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlatformPaymentMethod Method { get; set; }
    public string? Note { get; set; }
    public string? RecordedByName { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// Row of the "who runs out soon" list the platform dashboard shows.
public class ExpiringTenantDto
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? PlanName { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? PaidUntil { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    /// Days until whichever date applies (paid period for Active, trial for Trial).
    /// Negative means already past — the tenant is inside its grace period.
    public int DaysLeft { get; set; }
    public string Kind { get; set; } = "paid";   // "paid" | "trial"
}

// ── Suspension (S2) ──────────────────────────────────────────────────────────

public class SuspendTenantDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SuspendReason Reason { get; set; } = SuspendReason.Other;
    /// Internal note. Never returned to the tenant.
    public string? Note { get; set; }
    /// Shown to the tenant instead of the generic message.
    public string? PublicMessage { get; set; }
    /// Auto-reactivation date; null = until manually activated.
    public DateTime? Until { get; set; }
}

// ── Leads (S3, S7) ───────────────────────────────────────────────────────────

/// Public demo request — no authentication.
public class CreateLeadDto
{
    public string CompanyName { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Note { get; set; }
}

public class LeadDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Note { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LeadSource Source { get; set; }
    public int? ReferrerTenantId { get; set; }
    public string? ReferrerTenantName { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LeadStatus Status { get; set; }
    public string? StatusNote { get; set; }
    public int? ConvertedTenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateLeadDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LeadStatus Status { get; set; }
    public string? StatusNote { get; set; }
}

/// Turns a won lead into a real tenant. Same fields as creating a tenant by hand,
/// plus the paid-through date so billing starts immediately.
public class ConvertLeadDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string AdminFullName { get; set; } = null!;
    public string AdminPhone { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public int? PlanId { get; set; }
    public DateTime? PaidUntil { get; set; }
    /// Taxpayer id — links the new tenant to its platform-level Organization (S6).
    public string? Inn { get; set; }
}

/// Portal user asking for their own copy of the system (S7).
public class UpgradeInterestDto
{
    public string? Phone { get; set; }
    public string? Note { get; set; }
}

public class UpgradeInterestStatusDto
{
    public bool Submitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LeadStatus? Status { get; set; }
}

// ── Features (S4, S5) ────────────────────────────────────────────────────────

public class FeatureDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ModuleCode { get; set; }
    public bool DefaultEnabled { get; set; }
    public bool IsCustom { get; set; }
    public int? OwnerTenantId { get; set; }
    public string? OwnerTenantName { get; set; }
    public string? Reason { get; set; }
    public DateTime? RequestedAt { get; set; }
    public int SortOrder { get; set; }
}

public class CreateFeatureDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ModuleCode { get; set; }
    public bool DefaultEnabled { get; set; } = true;
    public bool IsCustom { get; set; }
    public int? OwnerTenantId { get; set; }
    public string? Reason { get; set; }
    public int SortOrder { get; set; }
}

/// A feature as it applies to one tenant, with where the value came from.
public class TenantFeatureDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ModuleCode { get; set; }
    public bool IsEnabled { get; set; }
    /// "tenant" (override) | "plan" | "default" | "module" (forced off by its module)
    public string Source { get; set; } = null!;
    public string? Note { get; set; }

    // Custom-feature provenance: the console warns before granting one client's feature
    // to a different client, and shows why it was built.
    public bool IsCustom { get; set; }
    public int? OwnerTenantId { get; set; }
    public string? OwnerTenantName { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Reason { get; set; }
}

public class SetTenantFeatureDto
{
    public string Code { get; set; } = null!;
    /// Null removes the override and falls back to the plan.
    public bool? IsEnabled { get; set; }
    public string? Note { get; set; }
}

public class SetTenantFeaturesDto
{
    public List<SetTenantFeatureDto> Features { get; set; } = new();
}

// ── Organizations (S6) ───────────────────────────────────────────────────────

public class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Inn { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int TenantCount { get; set; }
    public int CounterpartyCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrganizationDetailDto : OrganizationDto
{
    /// Tenants that ARE this company (it runs its own system).
    public List<OrganizationTenantDto> Tenants { get; set; } = new();
    /// Tenants that merely KNOW this company (it sits in their counterparty book).
    public List<OrganizationCounterpartyDto> Counterparties { get; set; } = new();
}

public class OrganizationTenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
}

public class OrganizationCounterpartyDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    /// The name this tenant filed the company under — rarely identical across tenants.
    public string CounterpartyName { get; set; } = null!;
}
