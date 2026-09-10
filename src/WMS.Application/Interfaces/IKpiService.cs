using WMS.Application.DTOs.Kpi;

namespace WMS.Application.Interfaces;

public interface IKpiService
{
    Task<List<ShiftDto>> GetShiftsAsync();
    Task<ShiftDto> CreateShiftAsync(CreateShiftDto dto);
    Task<ShiftDto> UpdateShiftAsync(Guid id, UpdateShiftDto dto);
    Task DeleteShiftAsync(Guid id);
    Task<List<ShiftPlanDto>> GetPlansAsync(DateTime? date = null);
    Task<ShiftPlanDto> CreatePlanAsync(CreateShiftPlanDto dto);
    Task<List<ShiftActualDto>> GetActualsAsync(DateTime? date = null);
    Task<ShiftActualDto> CreateActualAsync(CreateShiftActualDto dto);
    Task<KpiSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null);
    Task<List<EfficiencyDto>> GetEfficiencyAsync(DateTime? from = null, DateTime? to = null);
    Task<AttendanceLogDto> CheckInAsync(CheckInDto dto);
    Task<AttendanceLogDto> CheckOutAsync(Guid id);

    /// <param name="userId"><c>user_profile.id</c> bo'yicha filtr.</param>
    Task<List<AttendanceLogDto>> GetAttendanceAsync(Guid? userId = null, DateTime? date = null);
}
