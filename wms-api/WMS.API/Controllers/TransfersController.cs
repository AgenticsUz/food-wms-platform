using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[RequirePermission("transfers.view")]
[RequireModule(ModuleCodes.Transfers)]
public class TransfersController : BaseController
{
    private readonly ITransferService _transfers;
    public TransfersController(ITransferService transfers) => _transfers = transfers;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TransferType? type, [FromQuery] TransferStatus? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int? counterpartyId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<TransferDto>>.Ok(
            await _transfers.GetAllAsync(TenantId, type, status, from, to, counterpartyId, page, pageSize)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(ApiResponse<TransferDto>.Ok(await _transfers.GetByIdAsync(TenantId, id)));

    [HttpPost]
    [RequirePermission("transfers.create")]
    public async Task<IActionResult> Create([FromBody] CreateTransferDto dto)
    {
        try
        {
            var result = await _transfers.CreateAsync(TenantId, UserId, dto);
            return Ok(ApiResponse<TransferDto>.Ok(result));
        }
        catch (AppException ex) when (ex is PaymentRequiredException
                                      or ModuleDisabledException
                                      or FeatureDisabledException)
        {
            // Entitlement and subscription refusals carry their own status and code —
            // let the exception middleware map them instead of flattening to 400.
            throw;
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{id}/confirm")]
    [RequirePermission("transfers.confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            var result = await _transfers.ConfirmAsync(TenantId, id);
            return Ok(ApiResponse<TransferDto>.Ok(result));
        }
        catch (AppException ex) when (ex is PaymentRequiredException
                                      or ModuleDisabledException
                                      or FeatureDisabledException)
        {
            // Entitlement and subscription refusals carry their own status and code —
            // let the exception middleware map them instead of flattening to 400.
            throw;
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{id}/reject")]
    [RequirePermission("transfers.reject")]
    public async Task<IActionResult> Reject(int id)
        => Ok(ApiResponse<TransferDto>.Ok(await _transfers.RejectAsync(TenantId, id)));

    [HttpDelete("{id}")]
    [RequirePermission("transfers.create")]
    public async Task<IActionResult> Cancel(int id)
    { await _transfers.CancelAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Cancelled")); }
}
