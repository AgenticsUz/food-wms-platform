using WMS.Application.DTOs.Agents;

namespace WMS.Application.Interfaces;

// Agent portali metodlari (PortalLogin, GetPortal*) F6 da o'chdi — D8.
public interface IAgentService
{
    Task<List<AgentDto>> GetAllAsync();
    Task<AgentDto> GetByIdAsync(Guid id);
    Task<AgentDto> CreateAsync(CreateAgentDto dto);
    Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto);
    Task DeleteAsync(Guid id);

    Task<AgentSalesReportDto> GetSalesReportAsync(Guid agentId, DateTime? from, DateTime? to);
    Task<List<CommissionRecordDto>> GetCommissionsAsync(Guid agentId);

    /// <param name="userId">To'lovni yozgan <c>user_profile.id</c> (xarajat yozuvi uchun).</param>
    Task PayCommissionAsync(Guid userId, Guid agentId, PayCommissionDto dto);
    Task UpdateCommissionStatusAsync(Guid recordId, UpdateCommissionStatusDto dto);
}
