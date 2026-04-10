using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Module : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public int OrderNumber { get; set; }
    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
}
