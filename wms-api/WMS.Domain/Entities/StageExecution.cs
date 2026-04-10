using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class StageExecution : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public int RecipeStageId { get; set; }
    public RecipeStage RecipeStage { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; } = 0;
    public decimal ReworkQuantity { get; set; } = 0;
    public int? WorkerUserId { get; set; }
    public User? WorkerUser { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public StageExecutionStatus Status { get; set; } = StageExecutionStatus.Pending;
    public string? Note { get; set; }
    public ICollection<QcCheck> QcChecks { get; set; } = new List<QcCheck>();
}
