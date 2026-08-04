using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Plans;

public class PlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public List<string> ModuleCodes { get; set; } = new();
    public int MaxUsers { get; set; }
    public int MaxWarehouses { get; set; }
    public int MaxTransfersPerMonth { get; set; }
    public int TenantCount { get; set; }
    /// Trial uzunligi (kun). 0 = trial emas (pullik plan).
    public int TrialDays { get; set; }
    /// Self-service registratsiya shu planga bog'lanadi (bitta plan).
    public bool IsDefault { get; set; }
}

// Used for both create AND update.
public class CreatePlanDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> ModuleCodes { get; set; } = new();
    public int MaxUsers { get; set; } = 10;
    public int MaxWarehouses { get; set; } = 3;
    public int MaxTransfersPerMonth { get; set; } = 1000;
    public int TrialDays { get; set; }
    public bool IsDefault { get; set; }
}

public class AssignPlanDto
{
    public int PlanId { get; set; }
}

public class PlatformStatsDto
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TrialTenants { get; set; }
    public int SuspendedTenants { get; set; }
    public int TotalUsers { get; set; }
    public int NewTenantsThisMonth { get; set; }
    public List<MonthCountDto> MonthlyGrowth { get; set; } = new();
    public List<RecentTenantDto> RecentTenants { get; set; } = new();
}

public class MonthCountDto
{
    public string Month { get; set; } = null!;   // e.g. "Jan", "Feb"
    public int Count { get; set; }
}

public class RecentTenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ModuleInfoDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string ModuleCode { get; set; } = null!;
}
