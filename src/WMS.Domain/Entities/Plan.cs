using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// Platforma darajasidagi tarif (control plane) — <c>tenant_id</c> yo'q, RLS yo'q.
/// Console'ning WMS bo'limi boshqaradi.
/// </summary>
/// <remarks>
/// ⚠️ <c>ModuleCodes</c> F6 da O'CHDI (D6): modul yoqish endi faqat Identity
/// obunasida (<c>tenant_product.modules</c>). Plan narx, limitlar va feature'larni
/// beradi, modul esa undan yuqori qatlam — plan uni ocholmaydi.
/// </remarks>
public class Plan : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Plan beradigan feature kodlari (vergul bilan). Tenant override uni bosadi.</summary>
    public string FeatureCodes { get; set; } = "";

    /// <summary>Yangi tenantga JIT'da biriktiriladigan plan (trial). Bittagina bo'lishi kerak.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Trial uzunligi (kun). 0 — trial plan emas.</summary>
    public int TrialDays { get; set; }

    public int MaxUsers { get; set; } = 10;
    public int MaxWarehouses { get; set; } = 3;
    public int MaxTransfersPerMonth { get; set; } = 1000;

    /// <summary>AI so'rovlari kvotasi (oyiga). 0 — cheklanmagan.</summary>
    /// <remarks>
    /// ⚠️ 0 ning ma'nosi qolgan limitlar bilan bir xil (cheklanmagan), lekin bu yerda u
    /// «bepul» degani EMAS: AI'ni yoqish uchun baribir <c>ai.chat</c> feature'i kerak va
    /// undan yuqorida platforma darajasidagi kunlik dollar shifti (<c>Ai:DailyUsdCap</c>)
    /// turadi. Ya'ni kvota — tenantni tenantdan ajratuvchi o'lchov, xarajat to'ri emas.
    /// </remarks>
    public int MaxAiRequestsPerMonth { get; set; }
}
