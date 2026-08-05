using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class FeatureService : IFeatureService
{
    private readonly WmsDbContext _db;
    private readonly ITenantStateService _tenantState;

    public FeatureService(WmsDbContext db, ITenantStateService tenantState)
    {
        _db = db;
        _tenantState = tenantState;
    }

    public async Task<List<FeatureDto>> GetCatalogAsync(bool? isCustom = null)
    {
        var query = _db.Features.AsNoTracking().AsQueryable();
        if (isCustom.HasValue) query = query.Where(f => f.IsCustom == isCustom.Value);

        var features = await query.OrderBy(f => f.SortOrder).ThenBy(f => f.Code).ToListAsync();
        var tenantNames = await _db.Tenants.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name);

        return features.Select(f => new FeatureDto
        {
            Id = f.Id, Code = f.Code, Name = f.Name, Description = f.Description,
            ModuleCode = f.ModuleCode, DefaultEnabled = f.DefaultEnabled,
            IsCustom = f.IsCustom, OwnerTenantId = f.OwnerTenantId,
            OwnerTenantName = f.OwnerTenantId != null ? tenantNames.GetValueOrDefault(f.OwnerTenantId.Value) : null,
            Reason = f.Reason, RequestedAt = f.RequestedAt, SortOrder = f.SortOrder
        }).ToList();
    }

    public async Task<FeatureDto> CreateAsync(CreateFeatureDto dto)
    {
        var code = (dto.Code ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(code)) throw new AppException("Feature code is required");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new AppException("Feature name is required");
        if (await _db.Features.AnyAsync(f => f.Code == code))
            throw new AppException("A feature with this code already exists");

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
        }

        var feature = new Feature
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            ModuleCode = string.IsNullOrWhiteSpace(dto.ModuleCode) ? null : dto.ModuleCode.Trim(),
            DefaultEnabled = dto.DefaultEnabled,
            IsCustom = dto.IsCustom,
            OwnerTenantId = dto.OwnerTenantId,
            Reason = dto.Reason,
            RequestedAt = dto.IsCustom ? DateTime.UtcNow : null,
            SortOrder = dto.SortOrder
        };
        _db.Features.Add(feature);
        await _db.SaveChangesAsync();

        return (await GetCatalogAsync()).First(f => f.Code == code);
    }

    public async Task<List<TenantFeatureDto>> GetTenantFeaturesAsync(int tenantId)
    {
        var modules = await GetEnabledModulesAsync(tenantId);
        var resolved = await FeatureResolver.ResolveAsync(_db, tenantId, modules);

        // Owner names are looked up once — the console warns before granting a feature that
        // was written for a different client.
        var ownerIds = resolved.Where(f => f.OwnerTenantId != null)
            .Select(f => f.OwnerTenantId!.Value).Distinct().ToList();
        var ownerNames = ownerIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Tenants.AsNoTracking().Where(t => ownerIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name);

        return resolved.Select(f => new TenantFeatureDto
        {
            Code = f.Code, Name = f.Name, ModuleCode = f.ModuleCode,
            IsEnabled = f.IsEnabled, Source = f.Source.ToString().ToLowerInvariant(),
            Note = f.Note, IsCustom = f.IsCustom,
            OwnerTenantId = f.OwnerTenantId,
            OwnerTenantName = f.OwnerTenantId != null ? ownerNames.GetValueOrDefault(f.OwnerTenantId.Value) : null,
            RequestedAt = f.RequestedAt, Reason = f.Reason
        }).ToList();
    }

    public async Task SetTenantFeaturesAsync(int tenantId, SetTenantFeaturesDto dto, int setByUserId)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId))
            throw new NotFoundException("Tenant not found");

        foreach (var item in dto.Features)
        {
            var code = (item.Code ?? "").Trim();
            if (code.Length == 0) continue;

            var feature = await _db.Features.FirstOrDefaultAsync(f => f.Code == code)
                ?? throw new NotFoundException(Messages.FeatureNotFound, code);

            var existing = await _db.TenantFeatures
                .FirstOrDefaultAsync(tf => tf.TenantId == tenantId && tf.FeatureCode == feature.Code);

            if (item.IsEnabled == null)
            {
                // Removing the override hands the decision back to the plan.
                if (existing != null) _db.TenantFeatures.Remove(existing);
                continue;
            }

            if (existing != null)
            {
                existing.IsEnabled = item.IsEnabled.Value;
                existing.Note = item.Note;
                existing.SetByUserId = setByUserId > 0 ? setByUserId : null;
                existing.SetAt = DateTime.UtcNow;
            }
            else
            {
                _db.TenantFeatures.Add(new TenantFeature
                {
                    TenantId = tenantId,
                    FeatureCode = feature.Code,
                    IsEnabled = item.IsEnabled.Value,
                    Note = item.Note,
                    SetByUserId = setByUserId > 0 ? setByUserId : null,
                    SetAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        _tenantState.Invalidate(tenantId);
    }

    public async Task<List<string>> GetEnabledCodesAsync(int tenantId)
    {
        var modules = await GetEnabledModulesAsync(tenantId);
        var resolved = await FeatureResolver.ResolveAsync(_db, tenantId, modules);
        return resolved.Where(f => f.IsEnabled).Select(f => f.Code).ToList();
    }

    private async Task<HashSet<string>> GetEnabledModulesAsync(int tenantId)
    {
        var modules = await _db.TenantModules.AsNoTracking()
            .Where(tm => tm.TenantId == tenantId && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();
        return new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);
    }
}
