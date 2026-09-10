using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Trade;

[RequirePermission(WmsPermissions.PartnersView)]
[RequireModule(ModuleCodes.Suppliers, ModuleCodes.Clients)]
public class CounterpartiesController : BaseController
{
    private readonly ICounterpartyService _counterparties;
    public CounterpartiesController(ICounterpartyService counterparties) => _counterparties = counterparties;

    /// <remarks>Yetkazish yaratishda mijoz tanlanadi — `delivery.manage` ham yetadi (faqat ro'yxat).</remarks>
    [HttpGet]
    [RequireAnyPermission(WmsPermissions.PartnersView, WmsPermissions.DeliveryManage)]
    public async Task<IActionResult> GetAll([FromQuery] CounterpartyType? type)
        => Ok(ApiResponse<List<CounterpartyDto>>.Ok(await _counterparties.GetAllAsync(type)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<CounterpartyDto>.Ok(await _counterparties.GetByIdAsync(id)));

    [HttpPost]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> Create([FromBody] CreateCounterpartyDto dto)
        => Ok(ApiResponse<CounterpartyDto>.Ok(await _counterparties.CreateAsync(dto)));

    [HttpPut("{id}")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCounterpartyDto dto)
        => Ok(ApiResponse<CounterpartyDto>.Ok(await _counterparties.UpdateAsync(id, dto)));

    [HttpDelete("{id}")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _counterparties.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id}/balance")]
    public async Task<IActionResult> GetBalance(Guid id)
        => Ok(ApiResponse<CounterpartyBalanceDto>.Ok(await _counterparties.GetBalanceAsync(id)));

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(Guid id)
        => Ok(ApiResponse<List<PaymentHistoryDto>>.Ok(await _counterparties.GetPaymentsAsync(id)));
}
