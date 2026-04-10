using WMS.Application.DTOs.Qc;

namespace WMS.Application.Interfaces;

public interface IQcService
{
    Task<List<QcParameterDto>> GetParametersAsync(int tenantId);
    Task<QcParameterDto> CreateParameterAsync(int tenantId, CreateQcParameterDto dto);
    Task<List<QcCheckDto>> GetChecksAsync(int tenantId, int? transferId = null, int? stageExecutionId = null);
    Task<QcCheckDto> CreateCheckAsync(int tenantId, int userId, CreateQcCheckDto dto);
}
