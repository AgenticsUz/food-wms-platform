using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Kpi;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations;

// F6: tenant parametri o'chdi (D4). Kun bo'yicha filtrlar `x.Date.Date == d.Date` o'rniga
// [kun boshi; ertasi) oralig'i: indeks ishlaydi va sana UTC kuni sifatida talqin qilinadi
// (UtcDateTimeConverter bilan bir xil — wms-web sanani UTC'da yuboradi).
public class KpiService : IKpiService
{
    private readonly WmsDbContext _db;
    public KpiService(WmsDbContext db) => _db = db;

    public async Task<List<ShiftDto>> GetShiftsAsync()
    {
        // Postgres tartibni kafolatlamaydi; Guid v7 — yaratilish tartibi (SQLite'dagi kabi).
        return await _db.Shifts
            .OrderBy(s => s.Id)
            .Select(s => new ShiftDto { Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime })
            .ToListAsync();
    }

    public async Task<ShiftDto> CreateShiftAsync(CreateShiftDto dto)
    {
        var s = new Shift { Name = dto.Name, StartTime = dto.StartTime, EndTime = dto.EndTime };
        _db.Shifts.Add(s);
        await _db.SaveChangesAsync();
        return new ShiftDto { Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime };
    }

    public async Task<ShiftDto> UpdateShiftAsync(Guid id, UpdateShiftDto dto)
    {
        var s = await _db.Shifts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Shift not found");
        s.Name = dto.Name;
        s.StartTime = dto.StartTime;
        s.EndTime = dto.EndTime;
        await _db.SaveChangesAsync();
        return new ShiftDto { Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime };
    }

    public async Task DeleteShiftAsync(Guid id)
    {
        var s = await _db.Shifts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Shift not found");
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<ShiftPlanDto>> GetPlansAsync(DateTime? date)
    {
        var q = _db.ShiftPlans.AsQueryable();
        if (date.HasValue)
        {
            var (dayStart, dayEnd) = Day(date.Value);
            q = q.Where(p => p.Date >= dayStart && p.Date < dayEnd);
        }

        return await q.OrderByDescending(p => p.Date).Select(p => new ShiftPlanDto
        {
            Id = p.Id, ShiftId = p.ShiftId, ShiftName = p.Shift.Name,
            ProductId = p.ProductId, ProductName = p.Product.Name,
            PlannedQuantity = p.PlannedQuantity, Date = p.Date
        }).ToListAsync();
    }

    public async Task<ShiftPlanDto> CreatePlanAsync(CreateShiftPlanDto dto)
    {
        var shiftExists = await _db.Shifts.AnyAsync(s => s.Id == dto.ShiftId);
        if (!shiftExists) throw new NotFoundException("Shift not found");
        var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists) throw new NotFoundException("Product not found");

        var p = new ShiftPlan
        {
            ShiftId = dto.ShiftId, ProductId = dto.ProductId,
            PlannedQuantity = dto.PlannedQuantity, Date = dto.Date
        };
        _db.ShiftPlans.Add(p);
        await _db.SaveChangesAsync();

        return await _db.ShiftPlans.Where(x => x.Id == p.Id).Select(x => new ShiftPlanDto
        {
            Id = x.Id, ShiftId = x.ShiftId, ShiftName = x.Shift.Name,
            ProductId = x.ProductId, ProductName = x.Product.Name,
            PlannedQuantity = x.PlannedQuantity, Date = x.Date
        }).FirstAsync();
    }

    public async Task<List<ShiftActualDto>> GetActualsAsync(DateTime? date)
    {
        var q = _db.ShiftActuals.AsQueryable();
        if (date.HasValue)
        {
            var (dayStart, dayEnd) = Day(date.Value);
            q = q.Where(a => a.Date >= dayStart && a.Date < dayEnd);
        }

        return await q.OrderByDescending(a => a.Date).Select(a => new ShiftActualDto
        {
            Id = a.Id, ShiftId = a.ShiftId, ShiftName = a.Shift.Name,
            ProductId = a.ProductId, ProductName = a.Product.Name,
            ActualQuantity = a.ActualQuantity, WasteQuantity = a.WasteQuantity,
            Date = a.Date, Note = a.Note
        }).ToListAsync();
    }

    public async Task<ShiftActualDto> CreateActualAsync(CreateShiftActualDto dto)
    {
        var shiftExists = await _db.Shifts.AnyAsync(s => s.Id == dto.ShiftId);
        if (!shiftExists) throw new NotFoundException("Shift not found");
        var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists) throw new NotFoundException("Product not found");

        var a = new ShiftActual
        {
            ShiftId = dto.ShiftId, ProductId = dto.ProductId,
            ActualQuantity = dto.ActualQuantity, WasteQuantity = dto.WasteQuantity,
            Date = dto.Date, Note = dto.Note
        };
        _db.ShiftActuals.Add(a);
        await _db.SaveChangesAsync();

        return await _db.ShiftActuals.Where(x => x.Id == a.Id).Select(x => new ShiftActualDto
        {
            Id = x.Id, ShiftId = x.ShiftId, ShiftName = x.Shift.Name,
            ProductId = x.ProductId, ProductName = x.Product.Name,
            ActualQuantity = x.ActualQuantity, WasteQuantity = x.WasteQuantity,
            Date = x.Date, Note = x.Note
        }).FirstAsync();
    }

    public async Task<KpiSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to)
    {
        var (planQ, actualQ) = InRange(from, to);

        // Yig'indilar SQL'da va `numeric` da: SQLite davrida `double` ga aylantirib yig'ilardi
        // (SQLite decimal SUM'ni bilmasdi) va katta miqdorlarda kasr yo'qolardi.
        var totalPlanned = await planQ.SumAsync(p => p.PlannedQuantity);
        var totalActual = await actualQ.SumAsync(a => a.ActualQuantity);
        var totalWaste = await actualQ.SumAsync(a => a.WasteQuantity);

        return new KpiSummaryDto
        {
            TotalPlanned = totalPlanned, TotalActual = totalActual,
            EfficiencyPercent = totalPlanned > 0 ? Math.Round(totalActual / totalPlanned * 100, 1) : 0,
            TotalWaste = totalWaste,
            WastePercent = totalActual > 0 ? Math.Round(totalWaste / totalActual * 100, 1) : 0
        };
    }

    public async Task<List<EfficiencyDto>> GetEfficiencyAsync(DateTime? from, DateTime? to)
    {
        var (plans, actuals) = InRange(from, to);

        // SQLite davrida ikkala jadval to'liq xotiraga olinib, har reja uchun faktlar
        // qayta-qayta aylanib yig'ilardi (O(reja × fakt)). Endi — bog'langan SUM subso'rovi:
        // bir smena, bir mahsulot, bir (UTC) kun.
        var rows = await plans
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                ShiftName = p.Shift.Name,
                ProductName = p.Product.Name,
                p.Date,
                Planned = p.PlannedQuantity,
                Actual = actuals
                    .Where(a => a.ShiftId == p.ShiftId && a.ProductId == p.ProductId && a.Date.Date == p.Date.Date)
                    .Sum(a => a.ActualQuantity)
            })
            .ToListAsync();

        return rows.Select(r => new EfficiencyDto
        {
            ShiftName = r.ShiftName, ProductName = r.ProductName, Date = r.Date,
            Planned = r.Planned, Actual = r.Actual,
            EfficiencyPercent = r.Planned > 0 ? Math.Round(r.Actual / r.Planned * 100, 1) : 0
        }).ToList();
    }

    public async Task<AttendanceLogDto> CheckInAsync(CheckInDto dto)
    {
        var userExists = await _db.UserProfiles.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists) throw new NotFoundException("User not found");
        var shiftExists = await _db.Shifts.AnyAsync(s => s.Id == dto.ShiftId);
        if (!shiftExists) throw new NotFoundException("Shift not found");

        var log = new AttendanceLog
        {
            UserId = dto.UserId, ShiftId = dto.ShiftId,
            CheckIn = DateTime.UtcNow, Method = dto.Method, DeviceId = dto.DeviceId
        };
        _db.AttendanceLogs.Add(log);
        await _db.SaveChangesAsync();

        var result = await _db.AttendanceLogs.Include(l => l.User).Include(l => l.Shift).FirstAsync(l => l.Id == log.Id);
        return MapAttendance(result);
    }

    public async Task<AttendanceLogDto> CheckOutAsync(Guid id)
    {
        var log = await _db.AttendanceLogs.Include(l => l.User).Include(l => l.Shift)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Attendance log not found");
        log.CheckOut = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapAttendance(log);
    }

    public async Task<List<AttendanceLogDto>> GetAttendanceAsync(Guid? userId, DateTime? date)
    {
        var q = _db.AttendanceLogs.AsQueryable();
        if (userId.HasValue) q = q.Where(l => l.UserId == userId.Value);
        if (date.HasValue)
        {
            var (dayStart, dayEnd) = Day(date.Value);
            q = q.Where(l => l.CheckIn >= dayStart && l.CheckIn < dayEnd);
        }

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

    // ── Sana yordamchilari ──

    /// <summary>Reja va fakt so'rovlari — bir xil [from; to-kunining-oxiri) oralig'ida.</summary>
    private (IQueryable<ShiftPlan> Plans, IQueryable<ShiftActual> Actuals) InRange(DateTime? from, DateTime? to)
    {
        IQueryable<ShiftPlan> plans = _db.ShiftPlans;
        IQueryable<ShiftActual> actuals = _db.ShiftActuals;

        if (from.HasValue)
        {
            var fromUtc = ToUtc(from.Value);
            plans = plans.Where(p => p.Date >= fromUtc);
            actuals = actuals.Where(a => a.Date >= fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusive = Day(to.Value).End;
            plans = plans.Where(p => p.Date < toExclusive);
            actuals = actuals.Where(a => a.Date < toExclusive);
        }

        return (plans, actuals);
    }

    private static (DateTime Start, DateTime End) Day(DateTime value)
    {
        var start = DateTime.SpecifyKind(ToUtc(value).Date, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }

    /// <remarks>
    /// Model binding <c>...Z</c> li satrni <c>Local</c> ga aylantiradi, sof sanani esa
    /// <c>Unspecified</c> qoldiradi; ikkinchisi UTC deb olinadi (UtcDateTimeConverter bilan bir xil).
    /// </remarks>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
