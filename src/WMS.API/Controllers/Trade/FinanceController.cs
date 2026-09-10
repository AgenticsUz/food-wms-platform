using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Trade;

// `[ApiController]` va `[Authorize]` BaseController'dan meros — bu yerdagi takrori olib tashlandi.
[Route("api/finance")]
[RequirePermission(WmsPermissions.FinanceView)]
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
            await _finance.GetTransactionsAsync(type, from, to, page, pageSize)));

    [HttpPost("transactions")]
    [RequireFeature(FeatureCodes.FinanceTransactions)]
    [RequirePermission(WmsPermissions.FinanceManage)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto)
        => Ok(ApiResponse<TransactionDto>.Ok(await _finance.CreateTransactionAsync(UserId, dto)));

    [HttpDelete("transactions/{id}")]
    [RequireFeature(FeatureCodes.FinanceTransactions)]
    [RequirePermission(WmsPermissions.FinanceManage)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    { await _finance.DeleteTransactionAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("debts")]
    [RequireFeature(FeatureCodes.FinanceDebts)]
    public async Task<IActionResult> GetDebts()
        => Ok(ApiResponse<List<DebtDto>>.Ok(await _finance.GetDebtsAsync()));

    [HttpGet("payments")]
    [RequireFeature(FeatureCodes.FinancePayments)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] Guid? counterpartyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<PaymentHistoryDto>>.Ok(
            await _finance.GetPaymentsAsync(counterpartyId, page, pageSize)));

    [HttpPost("payments")]
    [RequireFeature(FeatureCodes.FinancePayments)]
    [RequirePermission(WmsPermissions.FinanceManage)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        => Ok(ApiResponse<PaymentHistoryDto>.Ok(await _finance.CreatePaymentAsync(UserId, dto)));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(ApiResponse<FinanceSummaryDto>.Ok(await _finance.GetSummaryAsync()));
}
