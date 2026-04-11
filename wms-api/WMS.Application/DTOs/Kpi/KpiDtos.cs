using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Kpi;

public class ShiftDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class CreateShiftDto
{
    public string Name { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class UpdateShiftDto
{
    public string Name { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ShiftPlanDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public DateTime Date { get; set; }
}

public class CreateShiftPlanDto
{
    public int ShiftId { get; set; }
    public int ProductId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public DateTime Date { get; set; }
}

public class ShiftActualDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}

public class CreateShiftActualDto
{
    public int ShiftId { get; set; }
    public int ProductId { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}

public class KpiSummaryDto
{
    public decimal TotalPlanned { get; set; }
    public decimal TotalActual { get; set; }
    public decimal EfficiencyPercent { get; set; }
    public decimal TotalWaste { get; set; }
    public decimal WastePercent { get; set; }
}

public class EfficiencyDto
{
    public string ShiftName { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Planned { get; set; }
    public decimal Actual { get; set; }
    public decimal EfficiencyPercent { get; set; }
}

public class AttendanceLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public AttendanceMethod Method { get; set; }
    public string? DeviceId { get; set; }
}

public class CheckInDto
{
    public int UserId { get; set; }
    public int ShiftId { get; set; }
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;
    public string? DeviceId { get; set; }
}
