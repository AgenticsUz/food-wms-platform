using WMS.Application.DTOs.Agents;

namespace WMS.Application.Interfaces;

public interface IAgentService
{
    Task<List<AgentDto>> GetAllAsync(int tenantId);
    Task<AgentDto> GetByIdAsync(int tenantId, int id);
    Task<AgentDto> CreateAsync(int tenantId, CreateAgentDto dto);
    Task<AgentDto> UpdateAsync(int tenantId, int id, UpdateAgentDto dto);
    Task DeleteAsync(int tenantId, int id);

    Task<AgentSalesReportDto> GetSalesReportAsync(int tenantId, int agentId, DateTime? from, DateTime? to);
    Task<List<CommissionRecordDto>> GetCommissionsAsync(int tenantId, int agentId);
    Task PayCommissionAsync(int tenantId, int userId, int agentId, PayCommissionDto dto);
    Task UpdateCommissionStatusAsync(int tenantId, int recordId, UpdateCommissionStatusDto dto);

    // Agent portal (self-service cabinet)
    Task<AgentPortalAuthResponseDto> PortalLoginAsync(AgentPortalLoginDto dto);
    Task<AgentPortalProfileDto> GetPortalProfileAsync(int agentId);
    Task<AgentSalesReportDto> GetPortalSalesReportAsync(int agentId, int tenantId);
    Task<List<CommissionRecordDto>> GetPortalCommissionsAsync(int agentId, int tenantId);
}
