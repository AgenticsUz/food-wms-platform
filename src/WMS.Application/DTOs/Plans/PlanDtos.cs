namespace WMS.Application.DTOs.Plans;

// F6 (D6): plan endi MODUL bermaydi — `ModuleCodes` maydoni ikkala DTO'dan ham O'CHDI. Modul
// Identity obunasida; plan narx, limitlar va feature to'plamini beradi. Qolgan maydon nomlari
// o'zgarmadi: `/api/subscription/plans` (wms-web) va Console bitta shaklni ko'radi.

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public List<string> FeatureCodes { get; set; } = new();
    public int MaxUsers { get; set; }
    public int MaxWarehouses { get; set; }
    public int MaxTransfersPerMonth { get; set; }
    public int TenantCount { get; set; }
    /// Trial uzunligi (kun). 0 = trial emas (pullik plan).
    public int TrialDays { get; set; }
    /// Yangi tenant birinchi tokenda (JIT) shu planga tushadi — bittagina bo'ladi.
    public bool IsDefault { get; set; }
}

// Yaratish VA tahrirlash uchun bitta tana.
public class CreatePlanDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    /// Bo'sh ro'yxat: yangi planda — barcha oddiy (custom bo'lmagan) feature'lar, tahrirlashda —
    /// planning mavjud to'plami o'zgarmaydi. Ro'yxat yuborilsa to'plam shunga almashadi.
    public List<string> FeatureCodes { get; set; } = new();
    public int MaxUsers { get; set; } = 10;
    public int MaxWarehouses { get; set; } = 3;
    public int MaxTransfersPerMonth { get; set; } = 1000;
    public int TrialDays { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary><c>PUT /admin/v1/tenants/{id}/plan</c>. <c>null</c> — planni olib tashlash (limitsiz, katalog sukuti).</summary>
public class AssignPlanDto
{
    public Guid? PlanId { get; set; }
}

/// <summary>
/// Console bosh sahifasi hisoblagichlari (<c>GET /admin/v1/stats</c>).
/// </summary>
/// <remarks>
/// SQLite davridagi <c>TotalUsers</c> O'CHDI: <c>user_profile</c> RLS ostida, tenantlar bo'yicha
/// umumiy son har tenantga alohida kirishni talab qilardi — bosh sahifa uchun juda qimmat.
/// Foydalanuvchi soni tenant kartasida (<c>usage.users</c>).
/// </remarks>
public class PlatformStatsDto
{
    public int Total { get; set; }
    public int Trial { get; set; }
    public int Active { get; set; }
    public int Suspended { get; set; }
    /// WMS o'chirgichi bilan o'chirilganlar (<c>tenant.is_active = false</c>).
    public int Inactive { get; set; }
    /// 7 kun ichida trial yoki to'langan davri tugaydiganlar.
    public int Expiring7d { get; set; }
    public int NewThisMonth { get; set; }
    public List<MonthCountDto> MonthlyGrowth { get; set; } = new();
}

public class MonthCountDto
{
    /// <c>yyyy-MM</c> — oy nomi tilga bog'liq, tarjimani Console qiladi.
    public string Month { get; set; } = null!;
    public int Count { get; set; }
}
