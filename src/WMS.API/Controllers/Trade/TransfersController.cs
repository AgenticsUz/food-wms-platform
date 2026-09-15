using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Trade;

[RequirePermission(WmsPermissions.TransfersView)]
[RequireModule(ModuleCodes.Transfers)]
public class TransfersController : BaseController
{
    private readonly ITransferService _transfers;
    public TransfersController(ITransferService transfers) => _transfers = transfers;

    /// <remarks>
    /// <c>from</c>/<c>to</c> — HUJJAT SANASI oralig'i (P2.3): ro'yxat va hisobot bir xil kunda
    /// bir xil hujjatlarni ko'rsatsin.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TransferType? type, [FromQuery] TransferStatus? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] Guid? counterpartyId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<TransferDto>>.Ok(
            await _transfers.GetAllAsync(type, status, from, to, counterpartyId, page, pageSize)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<TransferDto>.Ok(await _transfers.GetByIdAsync(id)));

    // SQLite davridagi `catch (Exception) → 400` o'chdi: u xmin to'qnashuvini (409, D13) ham 400 ga
    // yassilab yuborardi. Biznes xatolari (AppException oilasi) middleware'da o'z statusi va
    // tarjimasi bilan qaytadi.
    //
    // Manba OSHKORA `Ui`: u DTO'dan kelmaydi (mijoz o'zini «AI» deb ko'rsatolmasin) — bot va
    // AI oqimlari o'z qiymatini SHU metod emas, o'z chaqiruv joyidan beradi.
    [HttpPost]
    [RequirePermission(WmsPermissions.TransfersCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTransferDto dto)
        => Ok(ApiResponse<TransferDto>.Ok(await _transfers.CreateAsync(UserId, dto, DocumentSource.Ui)));

    [HttpPut("{id}/confirm")]
    [RequirePermission(WmsPermissions.TransfersConfirm)]
    public async Task<IActionResult> Confirm(Guid id)
        => Ok(ApiResponse<TransferDto>.Ok(await _transfers.ConfirmAsync(id)));

    [HttpPut("{id}/reject")]
    [RequirePermission(WmsPermissions.TransfersReject)]
    public async Task<IActionResult> Reject(Guid id)
        => Ok(ApiResponse<TransferDto>.Ok(await _transfers.RejectAsync(id)));

    [HttpDelete("{id}")]
    [RequirePermission(WmsPermissions.TransfersCreate)]
    public async Task<IActionResult> Cancel(Guid id)
    { await _transfers.CancelAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Cancelled")); }
}
