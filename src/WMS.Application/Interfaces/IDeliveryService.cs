using WMS.Application.DTOs.Delivery;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IDeliveryService
{
    // Vehicles
    Task<List<VehicleDto>> GetVehiclesAsync();
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto);
    Task<VehicleDto> UpdateVehicleAsync(Guid id, CreateVehicleDto dto);
    Task DeleteVehicleAsync(Guid id);

    // Drivers
    Task<List<DriverDto>> GetDriversAsync();
    Task<DriverDto> CreateDriverAsync(CreateDriverDto dto);
    Task<DriverDto> UpdateDriverAsync(Guid id, CreateDriverDto dto);
    Task DeleteDriverAsync(Guid id);

    // Deliveries
    Task<List<DeliveryDto>> GetDeliveriesAsync(DeliveryStatus? status);
    Task<DeliveryDto> GetDeliveryByIdAsync(Guid id);

    /// <param name="userId">Yaratuvchining <c>user_profile.id</c>'si.</param>
    Task<DeliveryDto> CreateDeliveryAsync(Guid userId, CreateDeliveryDto dto);
    Task<DeliveryDto> UpdateStatusAsync(Guid id, UpdateDeliveryStatusDto dto);
    Task DeleteDeliveryAsync(Guid id);

    // Stops
    Task<DeliveryDto> MarkStopDeliveredAsync(Guid deliveryId, Guid stopId);
    Task<DeliveryDto> MarkStopFailedAsync(Guid deliveryId, Guid stopId, string? note);
}
