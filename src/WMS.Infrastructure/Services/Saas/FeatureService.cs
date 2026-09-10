using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
public class FeatureService : IFeatureService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;
    private readonly ICurrentTenant _currentTenant;

    public FeatureService(WmsDbContext db, ITenantStateService tenantState, ICurrentTenant currentTenant)
    {
        _db = db;
        _tenantState = tenantState;
        _currentTenant = currentTenant;
    }

    public async Task<List<FeatureDto>> GetCatalogAsync(bool? isCustom = null, CancellationToken ct = default)
    {
        var query = _db.Features.AsNoTracking().AsQueryable();
        if (isCustom.HasValue) query = query.Where(f => f.IsCustom == isCustom.Value);

        var features = await query.OrderBy(f => f.SortOrder).ThenBy(f => f.Code).ToListAsync(ct);
        var ownerNames = await OwnerNamesAsync(features.Select(f => f.OwnerTenantId), ct);

        return features.Select(f => new FeatureDto
        {
            Id = f.Id, Code = f.Code, Name = f.Name, Description = f.Description,
            ModuleCode = f.ModuleCode, DefaultEnabled = f.DefaultEnabled,
            IsCustom = f.IsCustom, OwnerTenantId = f.OwnerTenantId,
            OwnerTenantName = f.OwnerTenantId != null ? ownerNames.GetValueOrDefault(f.OwnerTenantId.Value) : null,
            Reason = f.Reason, RequestedAt = f.RequestedAt, SortOrder = f.SortOrder
        }).ToList();
    }

    public async Task<FeatureDto> CreateAsync(CreateFeatureDto dto, CancellationToken ct = default)
    {
        var code = (dto.Code ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(code)) throw new AppException("Feature code is required");
        if (code.Length > 64) throw new AppException("Feature code must be at most {0} characters", 64);
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new AppException("Feature name is required");
        if (await _db.Features.AnyAsync(f => f.Code == code, ct))
            throw new AppException("A feature with this code already exists");

        var moduleCode = string.IsNullOrWhiteSpace(dto.ModuleCode) ? null : dto.ModuleCode.Trim().ToUpperInvariant();
        if (moduleCode is not null && !ModuleCodes.All.Contains(moduleCode, StringComparer.Ordinal))
            throw new AppException("Unknown module code '{0}'", moduleCode);

        // Rules that keep one-customer work from leaking into everyone's product (S5):
        // a custom feature is off unless explicitly granted, carries the tenant it was
        // written for, and lives under the "custom." prefix so it is obvious in every list.
        if (dto.IsCustom)
        {
            if (dto.DefaultEnabled)
                throw new AppException("A custom feature must have DefaultEnabled = false — it is granted per tenant, never by default");
            if (dto.OwnerTenantId == null)
                throw new AppException("A custom feature must name the tenant it was written for (OwnerTenantId)");
            if (!code.StartsWith(FeatureCodes.CustomPrefix, StringComparison.OrdinalIgnoreCase))
                throw new AppException(Messages.CustomFeaturePrefix, FeatureCodes.CustomPrefix);
            if (!await _db.Tenants.AnyAsync(t => t.Id == dto.OwnerTenantId, ct))
                throw new NotFoundException("Tenant not found");
        }

        var feature = new Feature
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            ModuleCode = moduleCode,
            DefaultEnabled = dto.DefaultEnabled,
            IsCustom = dto.IsCustom,
            OwnerTenantId = dto.IsCustom ? dto.OwnerTenantId : null,
            Reason = dto.Reason,
            RequestedAt = dto.IsCustom ? DateTime.UtcNow : null,
            SortOrder = dto.SortOrder
        };
        _db.Features.Add(feature);
        await _db.SaveChangesAsync(ct);

        return (await GetCatalogAsync(null, ct)).First(f => f.Code == code);
    }

    public async Task<List<TenantFeatureDto>> GetTenantFeaturesAsync(Guid tenantId, CancellationToken ct = default)
    {
        RequireContext(tenantId);

        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.PlanId, t.Modules })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tenant not found");

        // Modullar Identity obunasidan (nusxa) — SQLite davridagi TenantModule jadvali o'chdi (D6).
        var modules = new HashSet<string>(
            tenant.Modules.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
        var resolved = await FeatureResolver.ResolveAsync(_db, tenantId, tenant.PlanId, modules, ct);

        // Owner names are looked up once — the console warns before granting a feature that
        // was written for a different client.
        var ownerNames = await OwnerNamesAsync(resolved.Select(f => f.OwnerTenantId), ct);

        return resolved.Select(f => new TenantFeatureDto
        {
            Code = f.Code, Name = f.Name, ModuleCode = f.ModuleCode,
            Enabled = f.IsEnabled, Source = SourceName(f.Source),
            Note = f.Note, IsCustom = f.IsCustom,
            OwnerTenantId = f.OwnerTenantId,
            OwnerTenantName = f.OwnerTenantId != null ? ownerNames.GetValueOrDefault(f.OwnerTenantId.Value) : null,
            RequestedAt = f.RequestedAt, Reason = f.Reason
        }).ToList();
    }

    public async Task SetTenantFeaturesAsync(Guid tenantId, IReadOnlyList<FeatureOverrideInput> overrides, Guid? setBySub,
        CancellationToken ct = default)
    {
        RequireContext(tenantId);
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            throw new NotFoundException("Tenant not found");

        var catalog = await _db.Features.AsNoTracking().Select(f => f.Code).ToListAsync(ct);
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in catalog) known[code] = code;

        // Override'lar RLS ostida — kontekst route'dagi tenantda (RequireContext).
        var existing = await _db.TenantFeatures.Where(tf => tf.TenantId == tenantId).ToListAsync(ct);

        foreach (var item in overrides)
        {
            var requested = (item.Code ?? "").Trim();
            if (requested.Length == 0) continue;

            if (!known.TryGetValue(requested, out var code))
                throw new NotFoundException(Messages.FeatureNotFound, requested);

            var current = existing.FirstOrDefault(tf => string.Equals(tf.FeatureCode, code, StringComparison.OrdinalIgnoreCase));
            var note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim();

            if (item.Enabled == null)
            {
                // Removing the override hands the decision back to the plan.
                if (current != null) _db.TenantFeatures.Remove(current);
                continue;
            }

            if (current != null)
            {
                current.IsEnabled = item.Enabled.Value;
                current.Note = note;
                current.SetBySub = setBySub;
                current.SetAt = DateTime.UtcNow;
            }
            else
            {
                var added = new TenantFeature
                {
                    TenantId = tenantId,
                    FeatureCode = code,
                    IsEnabled = item.Enabled.Value,
                    Note = note,
                    SetBySub = setBySub,
                    SetAt = DateTime.UtcNow
                };
                _db.TenantFeatures.Add(added);
                existing.Add(added);
            }
        }

        await _db.SaveChangesAsync(ct);
        _tenantState.Invalidate(tenantId);
    }

    /// Wash'dagi nom: tenantga oshkora qiymat — "override". Console ikkala mahsulotda bitta mantiqni ishlatsin.
    private static string SourceName(FeatureSource source) => source switch
    {
        FeatureSource.Tenant => "override",
        FeatureSource.Plan => "plan",
        FeatureSource.Module => "module",
        _ => "default"
    };

    private async Task<Dictionary<Guid, string>> OwnerNamesAsync(IEnumerable<Guid?> ownerIds, CancellationToken ct)
    {
        var ids = ownerIds.Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
        return ids.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Tenants.AsNoTracking().Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);
    }

    /// Override'lar RLS ostida: boshqa kontekstda o'qish jimgina «override yo'q» ko'rsatardi.
    private void RequireContext(Guid tenantId)
    {
        if (_currentTenant.TenantId != tenantId)
            throw new InvalidOperationException(
                $"Tenant {tenantId} feature'lari uchun tenant konteksti o'rnatilmagan (AdminBaseController.UseTenant chaqirilmagan).");
    }
}
