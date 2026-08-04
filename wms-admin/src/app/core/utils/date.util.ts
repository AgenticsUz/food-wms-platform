/**
 * Backend sanalarni UTC'da saqlaydi. Datepicker esa mahalliy `Date` beradi —
 * uni to'g'ridan-to'g'ri `toISOString()` qilish sanani bir kun surib yuborishi mumkin.
 * Shuning uchun kun/oy/yilni mahalliy vaqtdan olib, UTC yarim tunga qo'yamiz.
 */
export function utcDateOnly(d: Date): string {
  return new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate())).toISOString();
}

/** Bugundan berilgan sanagacha qolgan to'liq kunlar. null → sana yo'q. */
export function daysUntil(iso: string | null | undefined): number | null {
  if (!iso) return null;
  const target = new Date(iso).getTime();
  if (Number.isNaN(target)) return null;
  return Math.ceil((target - Date.now()) / 86_400_000);
}
