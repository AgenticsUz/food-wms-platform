using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[RequirePermission("partners.view")]
[RequireModule(ModuleCodes.Suppliers, ModuleCodes.Clients)]
public class CounterpartiesController : BaseController
{
    private readonly ICounterpartyService _counterparties;
    public CounterpartiesController(ICounterpartyService counterparties) => _counterparties = counterparties;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CounterpartyType? type)
        => Ok(ApiResponse<List<CounterpartyDto>>.Ok(await _counterparties.GetAllAsync(TenantId, type)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(ApiResponse<CounterpartyDto>.Ok(await _counterparties.GetByIdAsync(TenantId, id)));

    [HttpPost]
    [RequirePermission("partners.manage")]
    public async Task<IActionResult> Create([FromBody] CreateCounterpartyDto dto)
        => Ok(ApiResponse<CounterpartyDto>.Ok(await _counterparties.CreateAsync(TenantId, dto)));

    [HttpPut("{id}")]
    [RequirePermission("partners.manage")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCounterpartyDto dto)
        => Ok(ApiResponse<CounterpartyDto>.Ok(await _counterparties.UpdateAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    [RequirePermission("partners.manage")]
    public async Task<IActionResult> Delete(int id)
    { await _counterparties.DeleteAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id}/balance")]
    public async Task<IActionResult> GetBalance(int id)
        => Ok(ApiResponse<CounterpartyBalanceDto>.Ok(await _counterparties.GetBalanceAsync(TenantId, id)));

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(int id)
        => Ok(ApiResponse<List<PaymentHistoryDto>>.Ok(await _counterparties.GetPaymentsAsync(TenantId, id)));
}
