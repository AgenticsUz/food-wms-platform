using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Kpi;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class KpiService : IKpiService
{
    private readonly WmsDbContext _db;
    public KpiService(WmsDbContext db) => _db = db;

    public async Task<List<ShiftDto>> GetShiftsAsync(int tenantId)
    {
        return await _db.Shifts.Where(s => s.TenantId == tenantId)
            .Select(s => new ShiftDto { Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime })
            .ToListAsync();
    }

    public async Task<ShiftDto> CreateShiftAsync(int tenantId, CreateShiftDto dto)
    {
        var s = new Shift { TenantId = tenantId, Name = dto.Name, StartTime = dto.StartTime, EndTime = dto.EndTime };
        _db.Shifts.Add(s);
        await _db.SaveChangesAsync();
        return new ShiftDto { Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime };
    }

    public async Task<List<ShiftPlanDto>> GetPlansAsync(int tenantId, DateTime? date)
    {
        var q = _db.ShiftPlans.Where(p => p.TenantId == tenantId)
            .Include(p => p.Shift).Include(p => p.Product).AsQueryable();
        if (date.HasValue) q = q.Where(p => p.Date.Date == date.Value.Date);

        return await q.OrderByDescending(p => p.Date).Select(p => new ShiftPlanDto
        {
            Id = p.Id, ShiftId = p.ShiftId, ShiftName = p.Shift.Name,
            ProductId = p.ProductId, ProductName = p.Product.Name,
            PlannedQuantity = p.PlannedQuantity, Date = p.Date
        }).ToListAsync();
    }

    public async Task<ShiftPlanDto> CreatePlanAsync(int tenantId, CreateShiftPlanDto dto)
    {
        var p = new ShiftPlan
        {
            TenantId = tenantId, ShiftId = dto.ShiftId, ProductId = dto.ProductId,
            PlannedQuantity = dto.PlannedQuantity, Date = dto.Date
        };
        _db.ShiftPlans.Add(p);
        await _db.SaveChangesAsync();

        var result = await _db.ShiftPlans.Include(x => x.Shift).Include(x => x.Product).FirstAsync(x => x.Id == p.Id);
        return new ShiftPlanDto
        {
            Id = result.Id, ShiftId = result.ShiftId, ShiftName = result.Shift.Name,
            ProductId = result.ProductId, ProductName = result.Product.Name,
            PlannedQuantity = result.PlannedQuantity, Date = result.Date
        };
    }

    public async Task<List<ShiftActualDto>> GetActualsAsync(int tenantId, DateTime? date)
    {
        var q = _db.ShiftActuals.Where(a => a.TenantId == tenantId)
            .Include(a => a.Shift).Include(a => a.Product).AsQueryable();
        if (date.HasValue) q = q.Where(a => a.Date.Date == date.Value.Date);

        return await q.OrderByDescending(a => a.Date).Select(a => new ShiftActualDto
        {
            Id = a.Id, ShiftId = a.ShiftId, ShiftName = a.Shift.Name,
            ProductId = a.ProductId, ProductName = a.Product.Name,
            ActualQuantity = a.ActualQuantity, WasteQuantity = a.WasteQuantity,
            Date = a.Date, Note = a.Note
        }).ToListAsync();
    }

    public async Task<ShiftActualDto> CreateActualAsync(int tenantId, CreateShiftActualDto dto)
    {
        var a = new ShiftActual
        {
            TenantId = tenantId, ShiftId = dto.ShiftId, ProductId = dto.ProductId,
            ActualQuantity = dto.ActualQuantity, WasteQuantity = dto.WasteQuantity,
            Date = dto.Date, Note = dto.Note
        };
        _db.ShiftActuals.Add(a);
        await _db.SaveChangesAsync();

        var result = await _db.ShiftActuals.Include(x => x.Shift).Include(x => x.Product).FirstAsync(x => x.Id == a.Id);
        return new ShiftActualDto
        {
            Id = result.Id, ShiftId = result.ShiftId, ShiftName = result.Shift.Name,
            ProductId = result.ProductId, ProductName = result.Product.Name,
            ActualQuantity = result.ActualQuantity, WasteQuantity = result.WasteQuantity,
            Date = result.Date, Note = result.Note
        };
    }

    public async Task<KpiSummaryDto> GetSummaryAsync(int tenantId, DateTime? from, DateTime? to)
    {
        var planQ = _db.ShiftPlans.Where(p => p.TenantId == tenantId);
        var actualQ = _db.ShiftActuals.Where(a => a.TenantId == tenantId);
        if (from.HasValue) { planQ = planQ.Where(p => p.Date >= from); actualQ = actualQ.Where(a => a.Date >= from); }
        if (to.HasValue) { planQ = planQ.Where(p => p.Date <= to); actualQ = actualQ.Where(a => a.Date <= to); }

        var totalPlanned = await planQ.SumAsync(p => (decimal?)p.PlannedQuantity) ?? 0;
        var totalActual = await actualQ.SumAsync(a => (decimal?)a.ActualQuantity) ?? 0;
        var totalWaste = await actualQ.SumAsync(a => (decimal?)a.WasteQuantity) ?? 0;

        return new KpiSummaryDto
        {
            TotalPlanned = totalPlanned, TotalActual = totalActual,
            EfficiencyPercent = totalPlanned > 0 ? Math.Round(totalActual / totalPlanned * 100, 1) : 0,
            TotalWaste = totalWaste,
            WastePercent = totalActual > 0 ? Math.Round(totalWaste / totalActual * 100, 1) : 0
        };
    }

    public async Task<List<EfficiencyDto>> GetEfficiencyAsync(int tenantId, DateTime? from, DateTime? to)
    {
        var plans = _db.ShiftPlans.Where(p => p.TenantId == tenantId)
            .Include(p => p.Shift).Include(p => p.Product).AsQueryable();
        var actuals = _db.ShiftActuals.Where(a => a.TenantId == tenantId).AsQueryable();
        if (from.HasValue) { plans = plans.Where(p => p.Date >= from); actuals = actuals.Where(a => a.Date >= from); }
        if (to.HasValue) { plans = plans.Where(p => p.Date <= to); actuals = actuals.Where(a => a.Date <= to); }

        var planList = await plans.ToListAsync();
        var actualList = await actuals.ToListAsync();

        return planList.Select(p =>
        {
            var actual = actualList.FirstOrDefault(a =>
                a.ShiftId == p.ShiftId && a.ProductId == p.ProductId && a.Date.Date == p.Date.Date);
            var actualQty = actual?.ActualQuantity ?? 0;
            return new EfficiencyDto
            {
                ShiftName = p.Shift.Name, ProductName = p.Product.Name, Date = p.Date,
                Planned = p.PlannedQuantity, Actual = actualQty,
                EfficiencyPercent = p.PlannedQuantity > 0 ? Math.Round(actualQty / p.PlannedQuantity * 100, 1) : 0
            };
        }).ToList();
    }

    public async Task<AttendanceLogDto> CheckInAsync(int tenantId, CheckInDto dto)
    {
        var log = new AttendanceLog
        {
            TenantId = tenantId, UserId = dto.UserId, ShiftId = dto.ShiftId,
            CheckIn = DateTime.UtcNow, Method = dto.Method, DeviceId = dto.DeviceId
        };
        _db.AttendanceLogs.Add(log);
        await _db.SaveChangesAsync();

        var result = await _db.AttendanceLogs.Include(l => l.User).Include(l => l.Shift).FirstAsync(l => l.Id == log.Id);
        return MapAttendance(result);
    }

    public async Task<AttendanceLogDto> CheckOutAsync(int tenantId, int id)
    {
        var log = await _db.AttendanceLogs.Include(l => l.User).Include(l => l.Shift)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId)
            ?? throw new Exception("Attendance log not found");
        log.CheckOut = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapAttendance(log);
    }

    public async Task<List<AttendanceLogDto>> GetAttendanceAsync(int tenantId, int? userId, DateTime? date)
    {
        var q = _db.AttendanceLogs.Where(l => l.TenantId == tenantId)
            .Include(l => l.User).Include(l => l.Shift).AsQueryable();
        if (userId.HasValue) q = q.Where(l => l.UserId == userId.Value);
        if (date.HasValue) q = q.Where(l => l.CheckIn.Date == date.Value.Date);

        return await q.OrderByDescending(l => l.CheckIn).Select(l => new AttendanceLogDto
        {
            Id = l.Id, UserId = l.UserId, UserName = l.User.FullName,
            ShiftId = l.ShiftId, ShiftName = l.Shift.Name,
            CheckIn = l.CheckIn, CheckOut = l.CheckOut,
            Method = l.Method, DeviceId = l.DeviceId
        }).ToListAsync();
    }

    private static AttendanceLogDto MapAttendance(AttendanceLog l) => new()
    {
        Id = l.Id, UserId = l.UserId, UserName = l.User.FullName,
        ShiftId = l.ShiftId, ShiftName = l.Shift.Name,
        CheckIn = l.CheckIn, CheckOut = l.CheckOut,
        Method = l.Method, DeviceId = l.DeviceId
    };
}
