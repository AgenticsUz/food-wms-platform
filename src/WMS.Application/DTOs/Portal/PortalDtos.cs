using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Portal;

// Kabinet (F9) — tizim foydalanuvchisi bo'lmagan odamning O'Z oldi-berdisi: mijoz,
// ta'minotchi, agent. ⚠️ Bu DTO'lar F6 da o'chgan `DTOs/Portal/PortalDtos.cs` ning
// o'rnini bosadi, lekin login/parol maydonlarisiz: hisob Identity'da (D8, P4).

/// <summary>Kabinet egasining turi — yuza qaysi bo'limlarni ko'rsatishini shu hal qiladi.</summary>
public enum PortalActorKind
{
    /// <summary>Kontragent (mijoz yoki ta'minotchi).</summary>
    Counterparty = 1,

    /// <summary>Savdo agenti.</summary>
    Agent = 2,
}

/// <summary><c>GET /api/portal/me</c> — men kimman va qaysi zavodning kabinetidaman.</summary>
public class PortalMeDto
{
    public Guid Id { get; set; }
    public PortalActorKind Kind { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>Kontragentda — <see cref="CounterpartyType"/>; agentda <see langword="null"/>.</summary>
    public CounterpartyType? CounterpartyType { get; set; }

    public string? Phone { get; set; }
    public string? Inn { get; set; }

    /// <summary>Zavod nomi — bir odam ikki zavodning kabinetiga kirishi mumkin.</summary>
    public string TenantName { get; set; } = null!;
}

/// <summary><c>GET /api/portal/finance</c> — balans va qisqacha yakun.</summary>
/// <remarks>
/// <c>DebtAmount</c> belgisi WMS qoidasidagi kabi: musbat — kontragent QARZDOR,
/// manfiy — zavod qarzdor (`DebtLedger`). Yuzada u shu ma'noda yoziladi.
/// </remarks>
public class PortalFinanceDto
{
    public decimal DebtAmount { get; set; }

    /// <summary>Tasdiqlangan chiqimlar summasi (ta'minotchida — kirimlar).</summary>
    public decimal TotalTurnover { get; set; }

    public decimal TotalPaid { get; set; }
    public DateTime? LastPaymentAt { get; set; }
}

/// <summary>Kabinetdagi bitta hujjat — ilova DTO'sining QISQARTIRILGAN nusxasi.</summary>
/// <remarks>
/// ⚠️ Ataylab alohida tur: ilovaning <c>TransferDto</c> sida ichki ma'lumot bor
/// (qaysi ombor, kim yaratdi, agent va uning foizi). Mijozga ular kerak emas va
/// ba'zisi tijorat siri — yuza kengaysa, u yerga tasodifan qo'shilib ketmasin.
/// </remarks>
public class PortalTransferDto
{
    public Guid Id { get; set; }
    public TransferType Type { get; set; }
    public TransferStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Hujjat sanasi — tovar HAQIQATDA kelgan/ketgan kun (P2.3).</summary>
    /// <remarks>
    /// ⚠️ Kabinet ro'yxati SHU maydon bo'yicha tartiblanadi. <see cref="CreatedAt"/>
    /// qoldirildi (mijoz «qachon rasmiylashtirildi» ni ham so'raydi), lekin ro'yxatdagi
    /// va zavoddagi tartib bir xil bo'lishi uchun asosiy sana shu.
    /// </remarks>
    public DateTime DocumentDate { get; set; }

    /// <summary>Qisqa hujjat raqami — mijoz telefonda «12-hujjat» deb ayta olsin (P2.4).</summary>
    public int Number { get; set; }

    public List<PortalTransferItemDto> Items { get; set; } = new();
}

public class PortalTransferItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>Kabinetdagi to'lov qatori (kim kiritgani KO'RSATILMAYDI — ichki ma'lumot).</summary>
public class PortalPaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>To'lov sanasi — <c>PaymentHistory.DocumentDate</c> (P2.3).</summary>
    /// <remarks>
    /// ⚠️ Entity'dagi <c>PaidAt</c> endi yozuv YARATILGAN lahza (audit izi). Mijozga «pul
    /// qachon o'tdi» kerak, «operator qachon kiritdi» emas — shuning uchun manba hujjat sanasi.
    /// </remarks>
    public DateTime PaidAt { get; set; }

    public string? Note { get; set; }
}

/// <summary><c>GET /api/portal/agent/summary</c> — agentning o'z ko'rsatkichlari.</summary>
public class PortalAgentSummaryDto
{
    public decimal CommissionPercent { get; set; }
    public int ClientCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal CommissionEarned { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal CommissionPending { get; set; }
}

/// <summary>Agent kabinetidagi mijoz qatori.</summary>
public class PortalAgentClientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public decimal DebtAmount { get; set; }
}

/// <summary>Kabinet hisobining holati — kontragent/agent kartasida ko'rsatiladi.</summary>
public class PortalAccountDto
{
    public bool Enabled { get; set; }

    /// <summary>Identity <c>sub</c> — «Kirish hisoblari» ro'yxatidagi odam bilan solishtirish uchun.</summary>
    public Guid? IdentitySub { get; set; }
}

/// <summary><c>PUT /api/counterparties/{id}/portal-account</c> — hisobni biriktirish.</summary>
/// <remarks>
/// Hisobning O'ZI Identity'da ochiladi (<c>/tenant/v1/members</c>, <c>role: client</c>) —
/// bu yerda faqat «qaysi hisob qaysi kontragent» yoziladi. Parol WMS'ga hech qachon kelmaydi.
/// </remarks>
public class LinkPortalAccountDto
{
    public Guid IdentitySub { get; set; }
}
