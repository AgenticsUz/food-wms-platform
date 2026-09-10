using WMS.Application.DTOs.Platform;

namespace WMS.Application.Interfaces;

/// <summary>
/// Qo'lda billing: tenant davr uchun to'laganini qayd etish va <c>PaidUntil</c> ni surish.
/// Console'ning WMS bo'limi chaqiradi (<c>/admin/v1</c>).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <c>payment_record</c> RLS ostida: <paramref name="tenantId"/> li metodlardan OLDIN chaqiruvchi
/// tenant kontekstini o'sha tenantga qo'ygan bo'lishi SHART (<c>AdminBaseController.UseTenant</c>).
/// Shu sababli o'chirish ham tenant ostida — global to'lov id'si bo'yicha kontekstsiz qidiruv 0 qator berardi.
/// </para>
/// <para>⚠️ F6 da O'CHDI: <c>ILeadService</c> (D9) va <c>IOrganizationService</c> (D10).</para>
/// </remarks>
public interface IBillingService
{
    Task<PaymentRecordDto> RecordPaymentAsync(Guid tenantId, RecordPaymentDto dto, Guid? recordedBySub, CancellationToken ct = default);
    Task<List<PaymentRecordDto>> GetPaymentsAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task DeletePaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct = default);

    /// <summary>Faqat <c>tenant</c> jadvalidan (platforma, RLS yo'q) — tenant konteksti kerak emas.</summary>
    Task<List<ExpiringTenantDto>> GetExpiringAsync(int days, CancellationToken ct = default);
}

/// <summary>Feature qatlami: katalog, tenant bo'yicha yechim va override'lar.</summary>
/// <remarks>
/// Katalog platforma jadvali; override'lar (<c>tenant_feature</c>) RLS ostida — tenantli metodlardan
/// oldin kontekst o'sha tenantga qo'yilgan bo'lsin. Tenantda HOZIR yoqilgan kodlar
/// <c>ITenantStateService</c> dan olinadi (SQLite davridagi <c>GetEnabledCodesAsync</c> o'chdi —
/// ikkinchi yo'l keshdan o'tmay, <c>/api/me</c> bilan ajralib qolardi).
/// </remarks>
public interface IFeatureService
{
    Task<List<FeatureDto>> GetCatalogAsync(bool? isCustom = null, CancellationToken ct = default);
    Task<FeatureDto> CreateAsync(CreateFeatureDto dto, CancellationToken ct = default);
    Task<List<TenantFeatureDto>> GetTenantFeaturesAsync(Guid tenantId, CancellationToken ct = default);
    Task SetTenantFeaturesAsync(Guid tenantId, IReadOnlyList<FeatureOverrideInput> overrides, Guid? setBySub, CancellationToken ct = default);
}
