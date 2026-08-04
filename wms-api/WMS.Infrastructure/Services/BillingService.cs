using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class BillingService : IBillingService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    private readonly SubscriptionOptions _options;

    public BillingService(WmsDbContext db, ITenantStateService tenantState,
        IOptions<SubscriptionOptions> options)
    {
        _db = db;
        _tenantState = tenantState;
        _options = options.Value;
    }

    public async Task<PaymentRecordDto> RecordPaymentAsync(int tenantId, RecordPaymentDto dto, int recordedByUserId)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new NotFoundException("Tenant not found");

        if (dto.PeriodEnd <= dto.PeriodStart)
            throw new AppException("Period end must be after period start");
        if (dto.Amount <= 0)
            throw new AppException("Amount must be greater than zero");

        var record = new PaymentRecord
        {
            TenantId = tenantId,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            Amount = dto.Amount,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "UZS" : dto.Currency.Trim().ToUpperInvariant(),
            Method = dto.Method,
            Note = dto.Note,
            RecordedByUserId = recordedByUserId > 0 ? recordedByUserId : null,
            RecordedAt = DateTime.UtcNow
        };
        _db.PaymentRecords.Add(record);

        // Paying extends the paid-through date, never shortens it: recording a forgotten
        // older period must not cut access that a later payment already granted.
        if (tenant.PaidUntil == null || dto.PeriodEnd > tenant.PaidUntil)
            tenant.PaidUntil = dto.PeriodEnd;

        // A tenant suspended for non-payment comes back by itself once it pays.
        // Suspensions for any other reason are left alone — money does not undo them.
        if (tenant.SubscriptionStatus == SubscriptionStatus.Suspended
            && tenant.SuspendReason == SuspendReason.NonPayment
            && tenant.PaidUntil > DateTime.UtcNow)
        {
            tenant.SubscriptionStatus = SubscriptionStatus.Active;
            tenant.SuspendReason = null;
            tenant.SuspendNote = null;
            tenant.SuspendPublicMessage = null;
            tenant.SuspendedUntil = null;
            tenant.SuspendedAt = null;
            tenant.SuspendedByUserId = null;
        }
        else if (tenant.SubscriptionStatus == SubscriptionStatus.Trial)
        {
            // First payment ends the trial.
            tenant.SubscriptionStatus = SubscriptionStatus.Active;
            tenant.TrialEndsAt = null;
        }

        await _db.SaveChangesAsync();
        _tenantState.Invalidate(tenantId);

        return await MapAsync(record, tenant.Name);
    }

    public async Task<List<PaymentRecordDto>> GetPaymentsAsync(int tenantId, int page, int pageSize)
    {
        var records = await _db.PaymentRecords
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.PeriodEnd)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var tenantName = await _db.Tenants.Where(t => t.Id == tenantId).Select(t => t.Name).FirstOrDefaultAsync();
        var result = new List<PaymentRecordDto>(records.Count);
        foreach (var record in records) result.Add(await MapAsync(record, tenantName));
        return result;
    }

    public async Task<int> DeletePaymentAsync(int paymentId)
    {
        var record = await _db.PaymentRecords.FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new NotFoundException("Payment record not found");

        record.IsDeleted = true;
        await _db.SaveChangesAsync();

        // PaidUntil is derived from the surviving records, so cancelling a mistaken entry
        // rolls the date back instead of leaving the tenant paid for a period it never was.
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == record.TenantId);
        if (tenant != null)
        {
            var latest = await _db.PaymentRecords
                .Where(p => p.TenantId == record.TenantId)
                .OrderByDescending(p => p.PeriodEnd)
                .Select(p => (DateTime?)p.PeriodEnd)
                .FirstOrDefaultAsync();
            tenant.PaidUntil = latest;
            await _db.SaveChangesAsync();
            _tenantState.Invalidate(tenant.Id);
        }

        return record.TenantId;
    }

    public async Task<List<ExpiringTenantDto>> GetExpiringAsync(int days)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(Math.Max(0, days));

        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive &&
                ((t.SubscriptionStatus == SubscriptionStatus.Active && t.PaidUntil != null && t.PaidUntil <= horizon)
                 || (t.SubscriptionStatus == SubscriptionStatus.Trial && t.TrialEndsAt != null && t.TrialEndsAt <= horizon)))
            .ToListAsync();

        var planNames = await _db.Plans.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name);

        return tenants.Select(t =>
        {
            var isTrial = t.SubscriptionStatus == SubscriptionStatus.Trial;
            var target = isTrial ? t.TrialEndsAt : t.PaidUntil;
            return new ExpiringTenantDto
            {
                TenantId = t.Id, Name = t.Name, Slug = t.Slug,
                PlanName = t.PlanId != null ? planNames.GetValueOrDefault(t.PlanId.Value) : null,
                Status = t.SubscriptionStatus,
                PaidUntil = t.PaidUntil, TrialEndsAt = t.TrialEndsAt,
                DaysLeft = target is { } d ? (int)Math.Ceiling((d - now).TotalDays) : 0,
                Kind = isTrial ? "trial" : "paid"
            };
        })
        .OrderBy(x => x.DaysLeft)
        .ToList();
    }

    private async Task<PaymentRecordDto> MapAsync(PaymentRecord record, string? tenantName)
    {
        string? recordedBy = null;
        if (record.RecordedByUserId is { } userId)
            recordedBy = await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync();

        return new PaymentRecordDto
        {
            Id = record.Id, TenantId = record.TenantId, TenantName = tenantName,
            PeriodStart = record.PeriodStart, PeriodEnd = record.PeriodEnd,
            Amount = record.Amount, Currency = record.Currency, Method = record.Method,
            Note = record.Note, RecordedByName = recordedBy, RecordedAt = record.RecordedAt
        };
    }
}
