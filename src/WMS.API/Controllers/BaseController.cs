using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

/// <summary>
/// Tenant yuzasi (<c>/api/*</c>, wms-web) controller'larining asosi.
/// </summary>
/// <remarks>
/// <para>
/// F6: <c>TenantId</c> xossasi ATAYLAB YO'Q (D4). Tenant RLS va <c>WmsDbContext</c> filtri bilan
/// avtomatik — servisga tenant uzatish endi kerak emas, uzatilgan qiymat esa noto'g'ri tenantga
/// yozish yo'lini ochardi.
/// </para>
/// <para>
/// <see cref="UserId"/> — joriy tenantdagi <c>user_profile.id</c> (Identity <c>sub</c> EMAS):
/// WMS jadvallaridagi har <c>*UserId</c> ustuni shunga ishora qiladi. SQLite davrida u tokendagi
/// <c>int</c> edi.
/// </para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireTenant]
public abstract class BaseController : ControllerBase
{
    protected ICurrentUser CurrentUser => HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    /// <summary>Joriy profil; profil yo'q (masalan platforma admini begona tenantda) — 403.</summary>
    protected Guid UserId => CurrentUser.ProfileId ?? throw new ForbiddenException("Your profile is not available in this organization");

    protected bool IsPlatformAdmin => CurrentUser.IsPlatformAdmin;
}
