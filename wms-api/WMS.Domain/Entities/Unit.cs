using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Unit : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}
