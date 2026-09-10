using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Tenants;

namespace WMS.Application.Interfaces;

/// <summary>
/// Tenant nusxasining WMS tijorat qatlami — Console'ning WMS bo'limi (<c>/admin/v1/tenants</c>).
/// </summary>
/// <remarks>
/// <para>
/// F6: WMS tenant YARATMAYDI va O'CHIRMAYDI (reyestr Identity'da, P4) va modul yoqmaydi (D6) —
/// SQLite davridagi <c>CreateAsync</c>/<c>DeleteAsync</c>/<c>Toggle*Module*</c> o'chdi. Qolgani:
/// nom, plan, trial, to'lov sanasi, suspend, WMS o'chirgichi.
/// </para>
/// <para>
/// ⚠️ <c>id</c> li metodlar karta yig'ayotganda RLS jadvallarini (feature override, profil, ombor,
/// transfer) ham o'qiydi — chaqiruvchi kontekstni o'sha tenantga qo'ygan bo'lishi SHART, aks holda
/// servis <see cref="InvalidOperationException"/> tashlaydi (jimgina 0 qator emas).
/// </para>
/// </remarks>
public interface ITenantService
{
    /// <param name="status"><c>trial</c> | <c>active</c> | <c>suspended</c> | <c>inactive</c> (WMS o'chirgichi); bo'sh — hammasi.</param>
    Task<(List<TenantListItemDto> Items, long Total)> ListAsync(int page, int size, string? search, string? status, CancellationToken ct = default);

    Task<TenantDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<TenantDetailDto> UpdateAsync(Guid id, UpdateTenantDto dto, CancellationToken ct = default);
    Task<TenantDetailDto> AssignPlanAsync(Guid id, Guid? planId, CancellationToken ct = default);
    Task<TenantDetailDto> SuspendAsync(Guid id, SuspendTenantDto? dto, Guid? suspendedBySub, CancellationToken ct = default);
    Task<TenantDetailDto> ActivateAsync(Guid id, CancellationToken ct = default);

    Task<PlatformStatsDto> GetStatsAsync(CancellationToken ct = default);
}
