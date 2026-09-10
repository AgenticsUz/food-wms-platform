using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Delivery;

// ── Vehicles ──

public class VehicleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Model { get; set; }
    public decimal Capacity { get; set; }
    public bool IsActive { get; set; }
}

public class CreateVehicleDto
{
    public string Name { get; set; } = null!;
    public string? Model { get; set; }
    public decimal Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Drivers ──

public class DriverDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; }
}

public class CreateDriverDto
{
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Deliveries ──

public class DeliveryDto
{
    public Guid Id { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleName { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? Note { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DeliveryStopDto> Stops { get; set; } = new();
    public int StopCount { get; set; }
    public int DeliveredCount { get; set; }
}

public class DeliveryStopDto
{
    public Guid Id { get; set; }
    public Guid CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public Guid? TransferId { get; set; }
    public string? Address { get; set; }
    public int SequenceOrder { get; set; }
    public DeliveryStopStatus Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Note { get; set; }
}

public class CreateDeliveryDto
{
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? Note { get; set; }
    public List<CreateDeliveryStopDto> Stops { get; set; } = new();
}

public class CreateDeliveryStopDto
{
    public Guid CounterpartyId { get; set; }
    public Guid? TransferId { get; set; }
    public string? Address { get; set; }
    public int SequenceOrder { get; set; }
    public string? Note { get; set; }
}

public class UpdateDeliveryStatusDto
{
    public DeliveryStatus Status { get; set; }
}

public class FailStopDto
{
    public string? Note { get; set; }
}
