using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Subscription;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

/// <summary>
/// Tenant o'z obunasini ko'radi: plan, holat, trial qolgan kuni, limitlar va joriy
/// foydalanish. Har foydalanuvchiga ochiq (banner/ogohlantirish uchun kerak) va
/// SubscriptionEnforcementMiddleware'dan ozod — bloklangan mijoz SABABINI ko'ra olishi shart.
/// </summary>
[Route("api/subscription")]
public class SubscriptionController : BaseController
{
    private readonly ISubscriptionService _subscription;
    public SubscriptionController(ISubscriptionService subscription) => _subscription = subscription;

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(ApiResponse<SubscriptionInfoDto>.Ok(await _subscription.GetForTenantAsync(TenantId, ct)));

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
        => Ok(ApiResponse<List<PlanDto>>.Ok(await _subscription.GetAvailablePlansAsync(ct)));
}
