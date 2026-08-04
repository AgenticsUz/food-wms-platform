using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/finance")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("finance.view")]
[RequireModule(ModuleCodes.Finance)]
public class FinanceController : BaseController
{
    private readonly IFinanceService _finance;
    public FinanceController(IFinanceService finance) => _finance = finance;

    [HttpGet("transactions")]
    [RequireFeature(FeatureCodes.FinanceTransactions)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] TransactionType? type, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(ApiResponse<List<TransactionDto>>.Ok(
            await _finance.GetTransactionsAsync(TenantId, type, from, to, page, pageSize)));

    [HttpPost("transactions")]
    [RequireFeature(FeatureCodes.FinanceTransactions)]
    [RequirePermission("finance.manage")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto)
        => Ok(ApiResponse<TransactionDto>.Ok(await _finance.CreateTransactionAsync(TenantId, UserId, dto)));

    [HttpDelete("transactions/{id}")]
    [RequireFeature(FeatureCodes.FinanceTransactions)]
    [RequirePermission("finance.manage")]
    public async Task<IActionResult> DeleteTransaction(int id)
    { await _finance.DeleteTransactionAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("debts")]
    [RequireFeature(FeatureCodes.FinanceDebts)]
    public async Task<IActionResult> GetDebts()
        => Ok(ApiResponse<List<DebtDto>>.Ok(await _finance.GetDebtsAsync(TenantId)));

    [HttpGet("payments")]
    [RequireFeature(FeatureCodes.FinancePayments)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int? counterpartyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<PaymentHistoryDto>>.Ok(
            await _finance.GetPaymentsAsync(TenantId, counterpartyId, page, pageSize)));

    [HttpPost("payments")]
    [RequireFeature(FeatureCodes.FinancePayments)]
    [RequirePermission("finance.manage")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        => Ok(ApiResponse<PaymentHistoryDto>.Ok(await _finance.CreatePaymentAsync(TenantId, UserId, dto)));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(ApiResponse<FinanceSummaryDto>.Ok(await _finance.GetSummaryAsync(TenantId)));
}
