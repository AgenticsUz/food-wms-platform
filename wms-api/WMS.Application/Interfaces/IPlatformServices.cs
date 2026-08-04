using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Tenants;

namespace WMS.Application.Interfaces;

/// Manual billing: recording that a tenant paid for a period and moving PaidUntil.
public interface IBillingService
{
    Task<PaymentRecordDto> RecordPaymentAsync(int tenantId, RecordPaymentDto dto, int recordedByUserId);
    Task<List<PaymentRecordDto>> GetPaymentsAsync(int tenantId, int page, int pageSize);
    /// Returns the tenant the payment belonged to, so the caller can audit against it.
    Task<int> DeletePaymentAsync(int paymentId);
    Task<List<ExpiringTenantDto>> GetExpiringAsync(int days);
}

/// Demo requests and the sales pipeline that replaced public self-service registration.
public interface ILeadService
{
    /// Public form. Repeated submissions from the same phone within 24h are folded into
    /// the existing lead instead of creating spam.
    Task<LeadDto> SubmitAsync(CreateLeadDto dto, Domain.Enums.LeadSource source);
    Task<List<LeadDto>> GetAllAsync(Domain.Enums.LeadStatus? status, Domain.Enums.LeadSource? source,
        string? search, int page, int pageSize);
    Task<LeadDto> GetByIdAsync(int id);
    Task<LeadDto> UpdateAsync(int id, UpdateLeadDto dto);
    Task<TenantDto> ConvertAsync(int id, ConvertLeadDto dto);

    // Portal upgrade interest (S7)
    Task<LeadDto> SubmitPortalInterestAsync(int tenantId, int? counterpartyId, int? agentId,
        string companyName, string contactName, UpgradeInterestDto dto);
    Task<UpgradeInterestStatusDto> GetPortalInterestAsync(int tenantId, int? counterpartyId, int? agentId);
}

/// The feature layer: catalog, per-tenant resolution and overrides.
public interface IFeatureService
{
    Task<List<FeatureDto>> GetCatalogAsync(bool? isCustom = null);
    Task<FeatureDto> CreateAsync(CreateFeatureDto dto);
    Task<List<TenantFeatureDto>> GetTenantFeaturesAsync(int tenantId);
    Task SetTenantFeaturesAsync(int tenantId, SetTenantFeaturesDto dto, int setByUserId);
    /// Codes currently enabled for a tenant — used by login and /api/subscription/me.
    Task<List<string>> GetEnabledCodesAsync(int tenantId);
}

/// Platform-level company identity (S6). Tenants never call this.
public interface IOrganizationService
{
    Task<List<OrganizationDto>> SearchAsync(string? search, int page, int pageSize);
    Task<OrganizationDetailDto> GetByIdAsync(int id);
}
