using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Qc;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class QcService : IQcService
{
    private readonly WmsDbContext _db;
    public QcService(WmsDbContext db) => _db = db;

    public async Task<List<QcParameterDto>> GetParametersAsync(int tenantId)
    {
        return await _db.QcParameters.Where(p => p.TenantId == tenantId)
            .Select(p => new QcParameterDto
            {
                Id = p.Id, Name = p.Name, Unit = p.Unit,
                MinValue = p.MinValue, MaxValue = p.MaxValue, ValueType = p.ValueType
            }).ToListAsync();
    }

    public async Task<QcParameterDto> CreateParameterAsync(int tenantId, CreateQcParameterDto dto)
    {
        var p = new QcParameter
        {
            TenantId = tenantId, Name = dto.Name, Unit = dto.Unit,
            MinValue = dto.MinValue, MaxValue = dto.MaxValue, ValueType = dto.ValueType
        };
        _db.QcParameters.Add(p);
        await _db.SaveChangesAsync();
        return new QcParameterDto
        {
            Id = p.Id, Name = p.Name, Unit = p.Unit,
            MinValue = p.MinValue, MaxValue = p.MaxValue, ValueType = p.ValueType
        };
    }

    public async Task<List<QcCheckDto>> GetChecksAsync(int tenantId, int? transferId, int? stageExecutionId)
    {
        var q = _db.QcChecks.Where(c => c.TenantId == tenantId)
            .Include(c => c.Parameter).Include(c => c.CheckedByUser).AsQueryable();
        if (transferId.HasValue) q = q.Where(c => c.TransferId == transferId.Value);
        if (stageExecutionId.HasValue) q = q.Where(c => c.StageExecutionId == stageExecutionId.Value);

        return await q.OrderByDescending(c => c.CheckedAt).Select(c => new QcCheckDto
        {
            Id = c.Id, StageExecutionId = c.StageExecutionId, TransferId = c.TransferId,
            ParameterId = c.ParameterId, ParameterName = c.Parameter.Name,
            Value = c.Value, IsPassed = c.IsPassed,
            CheckedByUserName = c.CheckedByUser.FullName, CheckedAt = c.CheckedAt, Note = c.Note
        }).ToListAsync();
    }

    public async Task<QcCheckDto> CreateCheckAsync(int tenantId, int userId, CreateQcCheckDto dto)
    {
        if (dto.StageExecutionId == null && dto.TransferId == null)
            throw new AppException("QC check must have either StageExecutionId or TransferId");
        if (dto.StageExecutionId != null && dto.TransferId != null)
            throw new AppException("QC check must have either StageExecutionId or TransferId, not both");

        if (dto.TransferId.HasValue)
        {
            var transferExists = await _db.Transfers
                .AnyAsync(t => t.Id == dto.TransferId.Value && t.TenantId == tenantId);
            if (!transferExists) throw new NotFoundException("Transfer not found");
        }

        if (dto.StageExecutionId.HasValue)
        {
            var stageExecutionExists = await _db.StageExecutions
                .AnyAsync(se => se.Id == dto.StageExecutionId.Value && se.ProductionOrder.TenantId == tenantId);
            if (!stageExecutionExists) throw new NotFoundException("Stage execution not found");
        }

        var parameterExists = await _db.QcParameters
            .AnyAsync(p => p.Id == dto.ParameterId && p.TenantId == tenantId);
        if (!parameterExists) throw new NotFoundException("QC parameter not found");

        var check = new QcCheck
        {
            TenantId = tenantId, StageExecutionId = dto.StageExecutionId,
            TransferId = dto.TransferId, ParameterId = dto.ParameterId,
            Value = dto.Value, IsPassed = dto.IsPassed,
            CheckedByUserId = userId, CheckedAt = DateTime.UtcNow, Note = dto.Note
        };
        _db.QcChecks.Add(check);
        await _db.SaveChangesAsync();

        var result = await _db.QcChecks.Include(c => c.Parameter).Include(c => c.CheckedByUser)
            .FirstAsync(c => c.Id == check.Id);
        return new QcCheckDto
        {
            Id = result.Id, StageExecutionId = result.StageExecutionId, TransferId = result.TransferId,
            ParameterId = result.ParameterId, ParameterName = result.Parameter.Name,
            Value = result.Value, IsPassed = result.IsPassed,
            CheckedByUserName = result.CheckedByUser.FullName, CheckedAt = result.CheckedAt, Note = result.Note
        };
    }
}
