using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class ProductionOrder : BaseEntity
{
    public int TenantId { get; set; }
    public int RecipeId { get; set; }
    public ProductionRecipe Recipe { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public string? Note { get; set; }
    public ICollection<StageExecution> StageExecutions { get; set; } = new List<StageExecution>();
}
