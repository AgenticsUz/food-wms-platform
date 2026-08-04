using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Delivery;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[Route("api/delivery")]
[RequirePermission("delivery.view")]
[RequireModule(ModuleCodes.Delivery)]
public class DeliveryController : BaseController
{
    private readonly IDeliveryService _delivery;
    private readonly IDeliveryPdfService _pdf;
    public DeliveryController(IDeliveryService delivery, IDeliveryPdfService pdf)
    { _delivery = delivery; _pdf = pdf; }

    // ── Vehicles ──

    [HttpGet("vehicles")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    public async Task<IActionResult> GetVehicles()
        => Ok(ApiResponse<List<VehicleDto>>.Ok(await _delivery.GetVehiclesAsync(TenantId)));

    [HttpPost("vehicles")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto dto)
        => Ok(ApiResponse<VehicleDto>.Ok(await _delivery.CreateVehicleAsync(TenantId, dto)));

    [HttpPut("vehicles/{id}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] CreateVehicleDto dto)
        => Ok(ApiResponse<VehicleDto>.Ok(await _delivery.UpdateVehicleAsync(TenantId, id, dto)));

    [HttpDelete("vehicles/{id}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> DeleteVehicle(int id)
    { await _delivery.DeleteVehicleAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Drivers ──

    [HttpGet("drivers")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    public async Task<IActionResult> GetDrivers()
        => Ok(ApiResponse<List<DriverDto>>.Ok(await _delivery.GetDriversAsync(TenantId)));

    [HttpPost("drivers")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDto dto)
        => Ok(ApiResponse<DriverDto>.Ok(await _delivery.CreateDriverAsync(TenantId, dto)));

    [HttpPut("drivers/{id}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> UpdateDriver(int id, [FromBody] CreateDriverDto dto)
        => Ok(ApiResponse<DriverDto>.Ok(await _delivery.UpdateDriverAsync(TenantId, id, dto)));

    [HttpDelete("drivers/{id}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> DeleteDriver(int id)
    { await _delivery.DeleteDriverAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Deliveries ──

    [HttpGet]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    public async Task<IActionResult> GetAll([FromQuery] DeliveryStatus? status)
        => Ok(ApiResponse<List<DeliveryDto>>.Ok(await _delivery.GetDeliveriesAsync(TenantId, status)));

    [HttpGet("{id}")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    public async Task<IActionResult> GetById(int id)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.GetDeliveryByIdAsync(TenantId, id)));

    [HttpPost]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.CreateDeliveryAsync(TenantId, UserId, dto)));

    [HttpPut("{id}/status")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeliveryStatusDto dto)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.UpdateStatusAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> Delete(int id)
    { await _delivery.DeleteDeliveryAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Stops ──

    [HttpPut("{id}/stops/{stopId}/deliver")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> MarkStopDelivered(int id, int stopId)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.MarkStopDeliveredAsync(TenantId, id, stopId)));

    [HttpPut("{id}/stops/{stopId}/fail")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission("delivery.manage")]
    public async Task<IActionResult> MarkStopFailed(int id, int stopId, [FromBody] FailStopDto? dto)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.MarkStopFailedAsync(TenantId, id, stopId, dto?.Note)));

    // ── Waybill PDF ──

    [HttpGet("{id}/waybill")]
    [RequireFeature(FeatureCodes.ExportPdf)]
    public async Task<IActionResult> Waybill(int id)
    {
        var bytes = await _pdf.GenerateWaybillPdfAsync(id, TenantId);
        return File(bytes, "application/pdf", $"delivery-{id}-waybill.pdf");
    }
}
