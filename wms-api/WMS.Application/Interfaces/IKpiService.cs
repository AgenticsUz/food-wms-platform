using WMS.Application.DTOs.Kpi;

namespace WMS.Application.Interfaces;

public interface IKpiService
{
    Task<List<ShiftDto>> GetShiftsAsync(int tenantId);
    Task<ShiftDto> CreateShiftAsync(int tenantId, CreateShiftDto dto);
    Task<List<ShiftPlanDto>> GetPlansAsync(int tenantId, DateTime? date = null);
    Task<ShiftPlanDto> CreatePlanAsync(int tenantId, CreateShiftPlanDto dto);
    Task<List<ShiftActualDto>> GetActualsAsync(int tenantId, DateTime? date = null);
    Task<ShiftActualDto> CreateActualAsync(int tenantId, CreateShiftActualDto dto);
    Task<KpiSummaryDto> GetSummaryAsync(int tenantId, DateTime? from = null, DateTime? to = null);
    Task<List<EfficiencyDto>> GetEfficiencyAsync(int tenantId, DateTime? from = null, DateTime? to = null);
    Task<AttendanceLogDto> CheckInAsync(int tenantId, CheckInDto dto);
    Task<AttendanceLogDto> CheckOutAsync(int tenantId, int id);
    Task<List<AttendanceLogDto>> GetAttendanceAsync(int tenantId, int? userId = null, DateTime? date = null);
}
