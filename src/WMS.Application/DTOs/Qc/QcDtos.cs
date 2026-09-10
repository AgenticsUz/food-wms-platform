using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Qc;

public class QcParameterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public QcParameterType ValueType { get; set; }
}

public class CreateQcParameterDto
{
    public string Name { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public QcParameterType ValueType { get; set; }
}

public class QcCheckDto
{
    public Guid Id { get; set; }
    public Guid? StageExecutionId { get; set; }
    public Guid? TransferId { get; set; }
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool IsPassed { get; set; }
    public string CheckedByUserName { get; set; } = null!;
    public DateTime CheckedAt { get; set; }
    public string? Note { get; set; }
}

public class CreateQcCheckDto
{
    public Guid? StageExecutionId { get; set; }
    public Guid? TransferId { get; set; }
    public Guid ParameterId { get; set; }
    public string Value { get; set; } = null!;
    public bool IsPassed { get; set; }
    public string? Note { get; set; }
}
