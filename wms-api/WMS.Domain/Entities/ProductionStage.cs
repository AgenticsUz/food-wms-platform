using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class ProductionStage : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public int OrderNumber { get; set; }
    public string? Description { get; set; }
}
