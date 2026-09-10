using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Infrastructure.Tenancy;

namespace WMS.API.Surfaces;

/// <summary>
/// Console yuzasi (<c>/admin/v1/*</c>) javob konverti — <c>{ data, meta, errors }</c>.
/// </summary>
/// <remarks>
/// ⚠️ Tenant yuzasidagi <c>ApiResponse</c> (<c>success/data/message</c>) EMAS: Console'ning
/// <c>ApiClient</c> i HRM va Wash'dan aynan shu shaklni kutadi va uni ochadi (D15). Xatolar
/// bu yuzada RFC 9457 ProblemDetails (<c>ExceptionHandlingMiddleware</c>).
/// </remarks>
public sealed record AdminResponse<T>(T? Data, AdminMeta? Meta, IReadOnlyList<AdminError> Errors)
{
    private static readonly IReadOnlyList<AdminError> NoErrors = [];

    public static AdminResponse<T> Ok(T data) => new(data, null, NoErrors);

    public static AdminResponse<T> Paged(T items, int page, int size, long total) => new(items, new AdminMeta(page, size, total), NoErrors);
}

public sealed record AdminMeta(int? Page, int? Size, long? Total);

public sealed record AdminError(string Code, string Message);

/// <summary>
/// Console yuzasi controller'larining asosi: siyosat (audience + Console operatori) va tenantni
/// OSHKORA tanlash.
/// </summary>
/// <remarks>
/// ⚠️ Marshrut har controller'da to'liq yoziladi (<c>[Route("admin/v1/...")]</c>): asosdagi
/// <c>[Route]</c> meros qoladi faqat avlod o'zinikini bermasa — ya'ni u yerda bir marta
/// unutilgan prefiks endpointni tenant yuzasiga tushirib yuborardi.
/// </remarks>
[ApiController]
[Authorize(Policy = WmsSurfaces.AdminPolicy)]
public abstract class AdminBaseController : ControllerBase
{
    /// <summary>
    /// Tenantni marshrutdan tanlaydi (<c>/admin/v1/tenants/{id}/...</c>) va RLS kontekstiga qo'yadi.
    /// </summary>
    /// <remarks>
    /// Console tokenida <c>tenant_id</c> yo'q (§3.3); platforma admini RLS'ni chetlab O'TMAYDI —
    /// tanlangan tenant aynan RLS sessiyasiga tushadi (§4.4). Marshrut sarlavhadan ishonchliroq:
    /// <c>X-Tenant-Id</c> ni Console hamma chaqiruvga qo'ymaydi (<c>ProductAdminApi</c> izohi).
    /// </remarks>
    protected Guid UseTenant(Guid tenantId)
    {
        HttpContext.RequestServices.GetRequiredService<ICurrentTenant>().Set(tenantId, null);
        return tenantId;
    }
}
