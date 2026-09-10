using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Delivery;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Operations;

[Route("api/delivery")]
[RequirePermission(WmsPermissions.DeliveryView)]
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
        => Ok(ApiResponse<List<VehicleDto>>.Ok(await _delivery.GetVehiclesAsync()));

    [HttpPost("vehicles")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto dto)
        => Ok(ApiResponse<VehicleDto>.Ok(await _delivery.CreateVehicleAsync(dto)));

    [HttpPut("vehicles/{id:guid}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] CreateVehicleDto dto)
        => Ok(ApiResponse<VehicleDto>.Ok(await _delivery.UpdateVehicleAsync(id, dto)));

    [HttpDelete("vehicles/{id:guid}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    { await _delivery.DeleteVehicleAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Drivers ──

    [HttpGet("drivers")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    public async Task<IActionResult> GetDrivers()
        => Ok(ApiResponse<List<DriverDto>>.Ok(await _delivery.GetDriversAsync()));

    [HttpPost("drivers")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDto dto)
        => Ok(ApiResponse<DriverDto>.Ok(await _delivery.CreateDriverAsync(dto)));

    [HttpPut("drivers/{id:guid}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> UpdateDriver(Guid id, [FromBody] CreateDriverDto dto)
        => Ok(ApiResponse<DriverDto>.Ok(await _delivery.UpdateDriverAsync(id, dto)));

    [HttpDelete("drivers/{id:guid}")]
    [RequireFeature(FeatureCodes.DeliveryFleet)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> DeleteDriver(Guid id)
    { await _delivery.DeleteDriverAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Deliveries ──

    [HttpGet]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    public async Task<IActionResult> GetAll([FromQuery] DeliveryStatus? status)
        => Ok(ApiResponse<List<DeliveryDto>>.Ok(await _delivery.GetDeliveriesAsync(status)));

    // `:guid` cheklovi: `vehicles`/`drivers` yo'llari bilan to'qnashmasin (int davrida
    // `{id}` satrni ham tutib, 400 berardi).
    [HttpGet("{id:guid}")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.GetDeliveryByIdAsync(id)));

    [HttpPost]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.CreateDeliveryAsync(UserId, dto)));

    [HttpPut("{id:guid}/status")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusDto dto)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.UpdateStatusAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _delivery.DeleteDeliveryAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Stops ──

    [HttpPut("{id:guid}/stops/{stopId:guid}/deliver")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> MarkStopDelivered(Guid id, Guid stopId)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.MarkStopDeliveredAsync(id, stopId)));

    [HttpPut("{id:guid}/stops/{stopId:guid}/fail")]
    [RequireFeature(FeatureCodes.DeliveryRoutes)]
    [RequirePermission(WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> MarkStopFailed(Guid id, Guid stopId, [FromBody] FailStopDto? dto)
        => Ok(ApiResponse<DeliveryDto>.Ok(await _delivery.MarkStopFailedAsync(id, stopId, dto?.Note)));

    // ── Waybill PDF ──

    [HttpGet("{id:guid}/waybill")]
    [RequireFeature(FeatureCodes.ExportPdf)]
    public async Task<IActionResult> Waybill(Guid id, CancellationToken ct)
    {
        var bytes = await _pdf.GenerateWaybillPdfAsync(id, ct);
        return File(bytes, "application/pdf", $"delivery-{id}-waybill.pdf");
    }
}
