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
}

// SuperAdmin tenant yaratadi: to'liq provizatsiya (admin user + modullar + rol)
public class CreateTenantDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string AdminFullName { get; set; } = null!;
    public string AdminPhone { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
}

public class UpdateTenantDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? PlanType { get; set; }
    public SubscriptionStatus? SubscriptionStatus { get; set; }
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
