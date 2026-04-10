using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class QcCheck : BaseEntity
{
    public int TenantId { get; set; }
    public int? StageExecutionId { get; set; }
    public StageExecution? StageExecution { get; set; }
    public int? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public int ParameterId { get; set; }
    public QcParameter Parameter { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool IsPassed { get; set; }
    public int CheckedByUserId { get; set; }
    public User CheckedByUser { get; set; } = null!;
    public DateTime CheckedAt { get; set; }
    public string? Note { get; set; }
}
