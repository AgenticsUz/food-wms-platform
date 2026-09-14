using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// Kontragent (yetkazib beruvchi / mijoz).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ F6 da O'CHDI: <c>OrganizationId</c> (global INN katalogi, D10 — RLS ostida
/// global jadval tenantlar orasida ma'lumot oqizardi) va portal login maydonlari
/// (<c>PortalPhone</c>/<c>PortalPasswordHash</c>, D8 — Identity'dan tashqari
/// ikkinchi foydalanuvchi reyestri P4 ga zid). INN kontragentning o'zida qoladi.
/// </para>
/// <para>
/// Kabinet QAYTDI, lekin parolsiz: <see cref="IdentitySub"/> — Identity'dagi
/// hisobga ishora, parol va bloklash o'sha yerda (D8 ning ikkinchi bosqichi,
/// sharti F8.1 bilan bajarildi).
/// </para>
/// </remarks>
public class Counterparty : TenantEntity
{
    public string Name { get; set; } = null!;
    public CounterpartyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }

    /// <summary>Mijozning standart agenti.</summary>
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    /// <summary>STIR (9 raqam), ixtiyoriy.</summary>
    public string? Inn { get; set; }

    /// <summary>
    /// Kabinet hisobi — Identity <c>sub</c>. <see langword="null"/> — kabinet ochilmagan.
    /// </summary>
    /// <remarks>
    /// ⚠️ Parol ham, bloklash ham bu yerda EMAS (P4): tenant admini hisobni «Kirish
    /// hisoblari» yuzasi orqali ochadi (<c>role: client</c>), bu ustun esa faqat
    /// «qaysi hisob qaysi kontragent» savoliga javob beradi. Tenantda bitta hisob
    /// bitta kontragentga — noyob indeks.
    /// </remarks>
    public Guid? IdentitySub { get; set; }
}

/// <summary>
/// Savdo agenti. ⚠️ Portal login maydonlari F6 da o'chdi (D8); kabinet
/// <see cref="IdentitySub"/> bilan qaytdi — parol Identity'da.
/// </summary>
public class Agent : TenantEntity
{
    /// <summary>Bog'langan WMS foydalanuvchisi (ixtiyoriy, <c>user_profile.id</c>).</summary>
    public Guid? UserId { get; set; }
    public UserProfile? User { get; set; }

    /// <summary>Kabinet hisobi — Identity <c>sub</c> (<c>Counterparty.IdentitySub</c> kabi).</summary>
    public Guid? IdentitySub { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }

    /// <summary>0..100.</summary>
    public decimal CommissionPercent { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Agent komissiyasi. ⚠️ <c>(tenant_id, transfer_id, agent_id)</c> bo'yicha NOYOB
/// (D13): SQLite davrida dublikatni faqat servisdagi <c>AnyAsync</c> to'sardi va
/// parallel tasdiqda ikkita yozuv tushishi mumkin edi. Qaytarishdagi manfiy yozuv
/// o'z qaytarish transferiga bog'lanadi, ya'ni noyoblikni buzmaydi.
/// </summary>
public class CommissionRecord : TenantEntity
{
    public Guid AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    public Guid TransferId { get; set; }
    public Transfer Transfer { get; set; } = null!;

    /// <summary>Sotuv summasi (nusxa).</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>Qo'llangan foiz (nusxa).</summary>
    public decimal CommissionPercent { get; set; }
    public decimal CommissionAmount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;

    /// <summary>Agentga to'lab berilgan.</summary>
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
}
