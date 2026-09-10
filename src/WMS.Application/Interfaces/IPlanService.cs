using WMS.Application.DTOs.Plans;

namespace WMS.Application.Interfaces;

/// <summary>Planlar katalogi — platforma jadvali, faqat Console (<c>/admin/v1/plans</c>) boshqaradi.</summary>
public interface IPlanService
{
    Task<List<PlanDto>> GetPlansAsync(CancellationToken ct = default);
    Task<PlanDto> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<PlanDto> UpdatePlanAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default);
    Task DeletePlanAsync(Guid id, CancellationToken ct = default);
}
