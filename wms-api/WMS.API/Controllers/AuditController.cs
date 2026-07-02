using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Audit;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[Route("api/audit")]
[RequirePermission("audit.view")]
public class AuditController : BaseController
{
    private readonly IAuditService _audit;
    public AuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<AuditLogDto>>.Ok(
            await _audit.GetLogsAsync(TenantId, entityType, from, to, page, pageSize)));

    [HttpGet("entity-types")]
    public async Task<IActionResult> GetEntityTypes()
        => Ok(ApiResponse<List<string>>.Ok(await _audit.GetEntityTypesAsync(TenantId)));
}
