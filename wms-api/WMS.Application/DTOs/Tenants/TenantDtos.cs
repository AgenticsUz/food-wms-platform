namespace WMS.Application.DTOs.Tenants;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CreateTenantDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
}

public class UpdateTenantDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; }
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
