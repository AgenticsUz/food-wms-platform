using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Counterparties;
using WMS.Application.DTOs.Finance;
using WMS.Application.DTOs.Notifications;
using WMS.Application.DTOs.Portal;
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

    // ── Mijoz Telegram'i (TG13): kartadan havola; xabarlar tenant sozlamasi (settings) yoqiq bo'lsagina ──

    [HttpGet("{id:guid}/telegram")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> GetTelegram(Guid id, [FromServices] ITelegramLinkService links, CancellationToken ct)
        => Ok(ApiResponse<TelegramSubjectLinkDto>.Ok(await links.GetSubjectLinkAsync(TelegramLinkSubject.Counterparty, id, ct)));

    [HttpPost("{id:guid}/telegram-link")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> CreateTelegramLink(Guid id, [FromServices] ITelegramLinkService links, CancellationToken ct)
        => Ok(ApiResponse<TelegramLinkTokenDto>.Ok(await links.CreateSubjectLinkTokenAsync(TelegramLinkSubject.Counterparty, id, ct)));

    [HttpDelete("{id:guid}/telegram")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> UnlinkTelegram(Guid id, [FromServices] ITelegramLinkService links, CancellationToken ct)
    { await links.UnlinkSubjectAsync(TelegramLinkSubject.Counterparty, id, ct); return Ok(ApiResponse<object>.Ok(null!, "Telegram disconnected")); }

    // ── Kabinet hisobi (F9): hisobni Identity'da SPA ochadi, bu yerda bog'lanish yoziladi ──

    [HttpGet("{id:guid}/portal-account")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> GetPortalAccount(Guid id, [FromServices] IPortalAccountService portal, CancellationToken ct)
        => Ok(ApiResponse<PortalAccountDto>.Ok(await portal.GetCounterpartyAccountAsync(id, ct)));

    /// <summary><c>{ identitySub }</c> — «Kirish hisoblari» qaytargan <c>userId</c>.</summary>
    [HttpPut("{id:guid}/portal-account")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> LinkPortalAccount(Guid id, [FromBody] LinkPortalAccountDto dto,
        [FromServices] IPortalAccountService portal, CancellationToken ct)
        => Ok(ApiResponse<PortalAccountDto>.Ok(await portal.LinkCounterpartyAsync(id, dto.IdentitySub, ct), "Portal access granted"));

    [HttpDelete("{id:guid}/portal-account")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public async Task<IActionResult> UnlinkPortalAccount(Guid id, [FromServices] IPortalAccountService portal, CancellationToken ct)
        => Ok(ApiResponse<PortalAccountDto>.Ok(await portal.UnlinkCounterpartyAsync(id, ct), "Portal access revoked"));

    [HttpGet("{id}/balance")]
    public async Task<IActionResult> GetBalance(Guid id)
        => Ok(ApiResponse<CounterpartyBalanceDto>.Ok(await _counterparties.GetBalanceAsync(id)));

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(Guid id)
        => Ok(ApiResponse<List<PaymentHistoryDto>>.Ok(await _counterparties.GetPaymentsAsync(id)));
}
