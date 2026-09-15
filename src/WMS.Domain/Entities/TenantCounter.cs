using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// Tenant ichidagi hujjat raqamlari hisoblagichi — har tur uchun bitta qator.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nega jadval, nega Postgres sequence emas.</b> Sequence tenantga bog'lanmaydi:
/// har tenantga alohida sequence yaratish kerak bo'lardi (yuzlab obyekt, migratsiyada
/// boshqarib bo'lmaydigan holat), bitta umumiy sequence esa raqamlarni tenantlar
/// orasida aralashtirib yuborardi («bizda 5 ta hujjat bor, nega oxirgisi #4187?»).
/// </para>
/// <para>
/// <b>Parallel yozuv.</b> Raqam <c>UPDATE … SET value = value + 1 RETURNING value</c>
/// bilan olinadi: Postgres qatorni qulflaydi, ya'ni ikki so'rov BIR XIL raqam ololmaydi.
/// Tranzaksiya qaytsa raqam yo'qoladi — bo'shliq bo'ladi va bu ATAYLAB
/// (bo'shliqsizlik uchun butun navbatni qulflash kerak bo'lardi).
/// </para>
/// </remarks>
public class TenantCounter : TenantEntity
{
    /// <summary>Hujjat turi — <see cref="Kinds"/> dagi qiymatlardan biri.</summary>
    public string Kind { get; set; } = null!;

    /// <summary>Oxirgi berilgan raqam.</summary>
    public int Value { get; set; }

    /// <summary>Hisoblagich turlari.</summary>
    public static class Kinds
    {
        /// <summary>Kirim/chiqim hujjatlari (<see cref="Transfer.Number"/>).</summary>
        public const string Transfer = "transfer";

        /// <summary>Ishlab chiqarish buyurtmalari (<see cref="ProductionOrder.Number"/>).</summary>
        public const string ProductionOrder = "production_order";
    }
}
