using WMS.Application.DTOs.Qc;

namespace WMS.Application.Interfaces;

public interface IQcService
{
    Task<List<QcParameterDto>> GetParametersAsync();
    Task<QcParameterDto> CreateParameterAsync(CreateQcParameterDto dto);
    Task<QcParameterDto> UpdateParameterAsync(Guid id, CreateQcParameterDto dto);
    Task DeleteParameterAsync(Guid id);
    Task<List<QcCheckDto>> GetChecksAsync(Guid? transferId = null, Guid? stageExecutionId = null);

    /// <param name="userId">Tekshiruvchining <c>user_profile.id</c> si (Identity <c>sub</c> emas).</param>
    Task<QcCheckDto> CreateCheckAsync(Guid userId, CreateQcCheckDto dto);
}
