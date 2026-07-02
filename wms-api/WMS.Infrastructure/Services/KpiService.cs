using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
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

    public async Task<ShiftDto> UpdateShiftAsync(int tenantId, int id, UpdateShiftDto dto)
    {
        var s = await _db.Shifts.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Shift not found");
        s.Name = dto.Name;
        s.StartTime = dto.StartTime;
        s.EndTime = dto.EndTime;
        await _db.SaveChangesAsync();
        return new ShiftDto { Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime };
    }

    public async Task DeleteShiftAsync(int tenantId, int id)
    {
        var s = await _db.Shifts.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Shift not found");
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
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
        if (to.HasValue)
        {
            var toExclusive = to.Value.Date.AddDays(1);
            planQ = planQ.Where(p => p.Date < toExclusive);
            actualQ = actualQ.Where(a => a.Date < toExclusive);
        }

        var totalPlanned = await planQ.SumAsync(p => (double)p.PlannedQuantity);
        var totalActual = await actualQ.SumAsync(a => (double)a.ActualQuantity);
        var totalWaste = await actualQ.SumAsync(a => (double)a.WasteQuantity);

        return new KpiSummaryDto
        {
            TotalPlanned = (decimal)totalPlanned, TotalActual = (decimal)totalActual,
            EfficiencyPercent = totalPlanned > 0 ? Math.Round((decimal)totalActual / (decimal)totalPlanned * 100, 1) : 0,
            TotalWaste = (decimal)totalWaste,
            WastePercent = totalActual > 0 ? Math.Round((decimal)totalWaste / (decimal)totalActual * 100, 1) : 0
        };
    }

    public async Task<List<EfficiencyDto>> GetEfficiencyAsync(int tenantId, DateTime? from, DateTime? to)
    {
        var plans = _db.ShiftPlans.Where(p => p.TenantId == tenantId)
            .Include(p => p.Shift).Include(p => p.Product).AsQueryable();
        var actuals = _db.ShiftActuals.Where(a => a.TenantId == tenantId).AsQueryable();
        if (from.HasValue) { plans = plans.Where(p => p.Date >= from); actuals = actuals.Where(a => a.Date >= from); }
        if (to.HasValue)
        {
            var toExclusive = to.Value.Date.AddDays(1);
            plans = plans.Where(p => p.Date < toExclusive);
            actuals = actuals.Where(a => a.Date < toExclusive);
        }

        var planList = await plans.ToListAsync();
        var actualList = await actuals.ToListAsync();

        return planList.Select(p =>
        {
            var actualQty = actualList
                .Where(a => a.ShiftId == p.ShiftId && a.ProductId == p.ProductId && a.Date.Date == p.Date.Date)
                .Sum(a => a.ActualQuantity);
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
        var userExists = await _db.Users.AnyAsync(u => u.Id == dto.UserId && u.TenantId == tenantId);
        if (!userExists) throw new NotFoundException("User not found");
        var shiftExists = await _db.Shifts.AnyAsync(s => s.Id == dto.ShiftId && s.TenantId == tenantId);
        if (!shiftExists) throw new NotFoundException("Shift not found");

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
            ?? throw new NotFoundException("Attendance log not found");
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
