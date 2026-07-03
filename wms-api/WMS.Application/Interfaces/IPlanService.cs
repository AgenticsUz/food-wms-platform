using WMS.Application.DTOs.Plans;

namespace WMS.Application.Interfaces;

public interface IPlanService
{
    Task<List<PlanDto>> GetPlansAsync();
    Task<PlanDto> CreatePlanAsync(CreatePlanDto dto);
    Task<PlanDto> UpdatePlanAsync(int id, CreatePlanDto dto);
    Task DeletePlanAsync(int id);
}
