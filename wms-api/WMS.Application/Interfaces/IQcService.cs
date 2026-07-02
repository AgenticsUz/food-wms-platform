using WMS.Application.DTOs.Qc;

namespace WMS.Application.Interfaces;

public interface IQcService
{
    Task<List<QcParameterDto>> GetParametersAsync(int tenantId);
    Task<QcParameterDto> CreateParameterAsync(int tenantId, CreateQcParameterDto dto);
    Task<QcParameterDto> UpdateParameterAsync(int tenantId, int id, CreateQcParameterDto dto);
    Task DeleteParameterAsync(int tenantId, int id);
    Task<List<QcCheckDto>> GetChecksAsync(int tenantId, int? transferId = null, int? stageExecutionId = null);
    Task<QcCheckDto> CreateCheckAsync(int tenantId, int userId, CreateQcCheckDto dto);
}
