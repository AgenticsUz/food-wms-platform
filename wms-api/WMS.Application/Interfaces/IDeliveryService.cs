using WMS.Application.DTOs.Delivery;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IDeliveryService
{
    // Vehicles
    Task<List<VehicleDto>> GetVehiclesAsync(int tenantId);
    Task<VehicleDto> CreateVehicleAsync(int tenantId, CreateVehicleDto dto);
    Task<VehicleDto> UpdateVehicleAsync(int tenantId, int id, CreateVehicleDto dto);
    Task DeleteVehicleAsync(int tenantId, int id);

    // Drivers
    Task<List<DriverDto>> GetDriversAsync(int tenantId);
    Task<DriverDto> CreateDriverAsync(int tenantId, CreateDriverDto dto);
    Task<DriverDto> UpdateDriverAsync(int tenantId, int id, CreateDriverDto dto);
    Task DeleteDriverAsync(int tenantId, int id);

    // Deliveries
    Task<List<DeliveryDto>> GetDeliveriesAsync(int tenantId, DeliveryStatus? status);
    Task<DeliveryDto> GetDeliveryByIdAsync(int tenantId, int id);
    Task<DeliveryDto> CreateDeliveryAsync(int tenantId, int userId, CreateDeliveryDto dto);
    Task<DeliveryDto> UpdateStatusAsync(int tenantId, int id, UpdateDeliveryStatusDto dto);
    Task DeleteDeliveryAsync(int tenantId, int id);

    // Stops
    Task<DeliveryDto> MarkStopDeliveredAsync(int tenantId, int deliveryId, int stopId);
    Task<DeliveryDto> MarkStopFailedAsync(int tenantId, int deliveryId, int stopId, string? note);
}
