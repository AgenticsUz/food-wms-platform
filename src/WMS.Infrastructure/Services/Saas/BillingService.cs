using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
public class BillingService : IBillingService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    private readonly ICurrentTenant _currentTenant;

    public BillingService(WmsDbContext db, ITenantStateService tenantState, ICurrentTenant currentTenant)
    {
        _db = db;
        _tenantState = tenantState;
        _currentTenant = currentTenant;
    }

    public async Task<PaymentRecordDto> RecordPaymentAsync(Guid tenantId, RecordPaymentDto dto, Guid? recordedBySub,
        CancellationToken ct = default)
    {
        RequireContext(tenantId);
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant not found");

        if (dto.PeriodTo <= dto.PeriodFrom)
            throw new AppException("Period end must be after period start");
        if (dto.Amount <= 0)
            throw new AppException("Amount must be greater than zero");

        var currency = string.IsNullOrWhiteSpace(dto.Currency) ? "UZS" : dto.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new AppException("Currency must be a three-letter code such as UZS");

        var record = new PaymentRecord
        {
            TenantId = tenantId,
            PeriodStart = dto.PeriodFrom,
            PeriodEnd = dto.PeriodTo,
            Amount = dto.Amount,
            Currency = currency,
            Method = dto.Method,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            RecordedBySub = recordedBySub,
            RecordedAt = DateTime.UtcNow
        };
        _db.PaymentRecords.Add(record);

        // Paying extends the paid-through date, never shortens it: recording a forgotten
        // older period must not cut access that a later payment already granted.
        if (tenant.PaidUntil == null || dto.PeriodTo > tenant.PaidUntil)
            tenant.PaidUntil = dto.PeriodTo;

        // A tenant suspended for non-payment comes back by itself once it pays.
        // Suspensions for any other reason are left alone — money does not undo them.
        if (tenant.SubscriptionStatus == SubscriptionStatus.Suspended
            && tenant.SuspendReason == SuspendReason.NonPayment
            && tenant.PaidUntil > DateTime.UtcNow)
        {
            tenant.SubscriptionStatus = SubscriptionStatus.Active;
            TenantService.ClearSuspension(tenant);
        }
        else if (tenant.SubscriptionStatus == SubscriptionStatus.Trial)
        {
            // First payment ends the trial.
            tenant.SubscriptionStatus = SubscriptionStatus.Active;
            tenant.TrialEndsAt = null;
        }

        await _db.SaveChangesAsync(ct);
        _tenantState.Invalidate(tenantId);

        return Map(record, tenant.Name);
    }

    public async Task<List<PaymentRecordDto>> GetPaymentsAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        RequireContext(tenantId);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var records = await _db.PaymentRecords
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.PeriodEnd).ThenByDescending(p => p.RecordedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var tenantName = await _db.Tenants.Where(t => t.Id == tenantId).Select(t => t.Name).FirstOrDefaultAsync(ct);
        return records.Select(r => Map(r, tenantName)).ToList();
    }

    public async Task DeletePaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct = default)
    {
        RequireContext(tenantId);
        var record = await _db.PaymentRecords.FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId, ct)
            ?? throw new NotFoundException("Payment record not found");

        record.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        // PaidUntil is derived from the surviving records, so cancelling a mistaken entry
        // rolls the date back instead of leaving the tenant paid for a period it never was.
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant != null)
        {
            tenant.PaidUntil = await _db.PaymentRecords
                .Where(p => p.TenantId == tenantId)
                .MaxAsync(p => (DateTime?)p.PeriodEnd, ct);
            await _db.SaveChangesAsync(ct);
            _tenantState.Invalidate(tenantId);
        }
    }

    public async Task<List<ExpiringTenantDto>> GetExpiringAsync(int days, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(Math.Clamp(days, 0, 365));

        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive &&
                ((t.SubscriptionStatus == SubscriptionStatus.Active && t.PlanId != null && t.PaidUntil != null && t.PaidUntil <= horizon)
                 || (t.SubscriptionStatus == SubscriptionStatus.Trial && t.TrialEndsAt != null && t.TrialEndsAt <= horizon)))
            .Select(t => new
            {
                t.Id, t.Code, t.Name, t.SubscriptionStatus, t.PaidUntil, t.TrialEndsAt,
                PlanCode = t.Plan != null ? t.Plan.Code : null,
                PlanName = t.Plan != null ? t.Plan.Name : null
            })
            .ToListAsync(ct);

        return tenants.Select(t =>
        {
            var isTrial = t.SubscriptionStatus == SubscriptionStatus.Trial;
            var target = isTrial ? t.TrialEndsAt : t.PaidUntil;
            return new ExpiringTenantDto
            {
                Id = t.Id, Code = t.Code, Name = t.Name,
                PlanCode = t.PlanCode, PlanName = t.PlanName,
                SubscriptionStatus = t.SubscriptionStatus,
                PaidUntil = t.PaidUntil, TrialEndsAt = t.TrialEndsAt,
                DaysLeft = target is { } d ? (int)Math.Ceiling((d - now).TotalDays) : 0,
                Kind = isTrial ? "trial" : "paid"
            };
        })
        .OrderBy(x => x.DaysLeft)
        .ToList();
    }

    private static PaymentRecordDto Map(PaymentRecord record, string? tenantName) => new()
    {
        Id = record.Id, TenantId = record.TenantId, TenantName = tenantName,
        PeriodFrom = record.PeriodStart, PeriodTo = record.PeriodEnd,
        Amount = record.Amount, Currency = record.Currency, Method = record.Method,
        Note = record.Note, RecordedBySub = record.RecordedBySub, RecordedAt = record.RecordedAt
    };

    /// <summary>
    /// <c>payment_record</c> RLS ostida: boshqa kontekstda yozish <c>StampEntries</c> da, o'qish esa
    /// jimgina 0 qator bilan tugardi («to'lovlar yo'q» degan yolg'on). Dasturchi xatosi — darhol yiqilsin.
    /// </summary>
    private void RequireContext(Guid tenantId)
    {
        if (_currentTenant.TenantId != tenantId)
            throw new InvalidOperationException(
                $"Tenant {tenantId} to'lovlari uchun tenant konteksti o'rnatilmagan (AdminBaseController.UseTenant chaqirilmagan).");
    }
}
