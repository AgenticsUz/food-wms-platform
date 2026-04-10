using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class QcParameter : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public QcParameterType ValueType { get; set; }
}
