using System.Text.Json.Serialization;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Tenants;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? PlanType { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int? PlanId { get; set; }
    public string? PlanName { get; set; }
    public DateTime? TrialEndsAt { get; set; }

    // Manual billing + suspension detail (S1, S2). SuspendNote is internal — it is returned
    // here because this DTO is SuperAdmin-only; it must never reach a tenant response.
    public DateTime? PaidUntil { get; set; }
    /// Name, not number ("ClientRequest") — same vocabulary the suspend request uses.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SuspendReason? SuspendReason { get; set; }
    public string? SuspendNote { get; set; }
    public string? SuspendPublicMessage { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public DateTime? SuspendedAt { get; set; }

    /// Taxpayer id of the linked Organization (S6) — shown and edited in the console.
    public string? Inn { get; set; }
    public int? OrganizationId { get; set; }

    /// Visual identity (B1). Logos are uploaded through their own endpoint; the colour
    /// travels with the ordinary tenant update.
    public string? LogoUrl { get; set; }
    public string? LogoSquareUrl { get; set; }
    public string? BrandColor { get; set; }
}

// SuperAdmin tenant yaratadi: to'liq provizatsiya (admin user + modullar + rol)
public class CreateTenantDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string AdminFullName { get; set; } = null!;
    public string AdminPhone { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public int? PlanId { get; set; }
    /// Optional taxpayer id — links the new tenant to its platform-level Organization (S6).
    public string? Inn { get; set; }
    /// Paid through this date from day one (a tenant is usually created after payment).
    public DateTime? PaidUntil { get; set; }
}

public class UpdateTenantDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? PlanType { get; set; }
    public SubscriptionStatus? SubscriptionStatus { get; set; }
    public int? PlanId { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? PaidUntil { get; set; }
    /// Sending an INN links (or re-links) the tenant to its Organization; sending an empty
    /// string unlinks it. Omitting the field leaves the link untouched.
    public string? Inn { get; set; }
    /// #RRGGBB, or an empty string to fall back to the default theme. Omitted = unchanged.
    public string? BrandColor { get; set; }
}

public class TenantModuleDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string ModuleCode { get; set; } = null!;
    public bool IsEnabled { get; set; }
}

public class ToggleModuleDto
{
    public int ModuleId { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Accepts both single module toggle and batch toggle from frontend.
/// Frontend sends: { modules: [{ moduleId, isEnabled }] }
/// </summary>
public class ToggleModulesRequest
{
    public List<ToggleModuleDto>? Modules { get; set; }
    // Fallback for single module toggle
    public int ModuleId { get; set; }
    public bool IsEnabled { get; set; }
}
