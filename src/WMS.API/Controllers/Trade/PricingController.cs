using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Pricing;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Trade;

/// <summary>
/// Narx takliflari (P2.2) — transfer formasi narx maydonining yordamchisi.
/// </summary>
/// <remarks>
/// Ruxsat va modul AYNAN <c>TransfersController</c> dagidek: bu yerdan qaytadigan narx —
/// hujjatlardagi ma'lumot, ya'ni hujjatlarni ko'ra olmaydigan odam uni bilmasligi kerak
/// (narx ro'yxati sir bo'lgan tenantlarda bu aylanma yo'l bo'lardi).
/// </remarks>
[RequirePermission(WmsPermissions.TransfersView)]
[RequireModule(ModuleCodes.Transfers)]
public class PricingController : BaseController
{
    private readonly IPricingService _pricing;

    /// <summary>Servisni oladi.</summary>
    /// <param name="pricing">Narx servisi.</param>
    public PricingController(IPricingService pricing) => _pricing = pricing;

    /// <summary>Oxirgi tasdiqlangan hujjatdagi narx.</summary>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="type">Hujjat turi (<c>Outgoing</c> — sotuv, <c>Incoming</c> — kirim).</param>
    /// <param name="counterpartyId">Kontragent (ixtiyoriy) — u bilan bo'lgan narx ustun.</param>
    /// <param name="cancellationToken">So'rov uzilsa.</param>
    /// <returns>Narx va manbasi; topilmasa <c>Data = null</c> (xato EMAS).</returns>
    [HttpGet("last-price")]
    public async Task<IActionResult> GetLastPrice(
        [FromQuery] Guid productId,
        [FromQuery] TransferType type,
        [FromQuery] Guid? counterpartyId,
        CancellationToken cancellationToken)
        => Ok(ApiResponse<LastPriceDto?>.Ok(
            await _pricing.GetLastPriceAsync(productId, counterpartyId, type, cancellationToken)));
}
