/**
 * Sana / vaqt zonasi helperlari.
 * Backend UTC ishlaydi, foydalanuvchi UTC+5 (O'zbekiston).
 */

/**
 * Mahalliy sanani `YYYY-MM-DD` stringiga aylantiradi (vaqt zonasi siljishisiz).
 * `toISOString().split('T')[0]` mahalliy yarim tunni oldingi UTC kuniga surib yuboradi —
 * shu funksiya buni oldini oladi.
 */
export function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * Backend'dan kelgan sanani UTC deb parse qiladi.
 * Backend `Z` suffiksisiz DateTime yuborsa, `new Date(s)` uni MAHALLIY deb o'qiydi
 * ("hozirgina" → "5 soat oldin"). Bu funksiya `Z` qo'shib to'g'ri UTC parse qiladi.
 */
export function parseUtc(s: string | null | undefined): Date | null {
  if (!s) return null;
  // ISO bo'lsa va zona ma'lumoti yo'q bo'lsa (Z yoki +hh:mm) — UTC deb belgilaymiz
  const hasZone = /[zZ]$/.test(s) || /[+-]\d{2}:?\d{2}$/.test(s);
  return new Date(hasZone ? s : s + 'Z');
}
