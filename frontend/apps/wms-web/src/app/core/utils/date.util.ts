/**
 * Sana / vaqt zonasi yordamchilari (eski `wms-ui/shared/utils/date.util.ts`).
 *
 * QOIDA: backend UTC'da ishlaydi, foydalanuvchi UTC+5 (O'zbekiston).
 *
 *  - VAQT NUQTASI (yaratilgan payt, `from`/`to` filtrining aniq vaqti) serverga
 *    UTC ISO satri bo'lib ketadi: `toUtcIso(date)`. `ApiService` so'rov
 *    parametridagi `Date` ni o'zi shunday o'giradi, JSON tanadagi `Date` esa
 *    `JSON.stringify` orqali baribir UTC ISO bo'ladi.
 *  - FAQAT SANA (hisobot kuni, muddat) — `toLocalDateString(date)` → `YYYY-MM-DD`,
 *    mahalliy kalendar kuni bo'yicha. ⚠️ `toISOString().split('T')[0]` ISHLATILMAYDI:
 *    Toshkentda yarim tundan 05:00 gacha u OLDINGI kunni beradi.
 *  - SERVERDAN KELGAN vaqt — `parseUtc(s)`: `Z` siz kelsa ham UTC deb o'qiladi.
 */

/** Mahalliy sanani `YYYY-MM-DD` ga aylantiradi (vaqt zonasi siljishisiz). */
export function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Vaqt nuqtasini serverga yuboriladigan UTC ISO satriga aylantiradi. */
export function toUtcIso(d: Date): string {
  return d.toISOString();
}

/**
 * Mahalliy kun oralig'ini UTC vaqt nuqtalariga aylantiradi: `from` kunining
 * 00:00:00 va `to` kunining 23:59:59.999 (mahalliy) → UTC ISO.
 *
 * Nega kerak: «10-sentabr» filtri serverda UTC bo'yicha solishtiriladi; mahalliy
 * yarim tun esa UTC'da oldingi kunning 19:00 i. Oraliq shu tarzda berilmasa
 * kunning birinchi besh soatidagi yozuvlar hisobotdan tushib qolardi.
 */
export function localDayRangeToUtc(from: Date, to: Date): { from: string; to: string } {
  const start = new Date(from.getFullYear(), from.getMonth(), from.getDate(), 0, 0, 0, 0);
  const end = new Date(to.getFullYear(), to.getMonth(), to.getDate(), 23, 59, 59, 999);
  return { from: start.toISOString(), to: end.toISOString() };
}

/**
 * Backend'dan kelgan sanani UTC deb parse qiladi.
 * `Z` suffiksisiz DateTime'ni `new Date(s)` MAHALLIY deb o'qiydi ("hozirgina" →
 * "5 soat oldin"). Zona ma'lumoti yo'q bo'lsa `Z` qo'shiladi.
 */
export function parseUtc(s: string | null | undefined): Date | null {
  if (!s) return null;
  const hasZone = /[zZ]$/.test(s) || /[+-]\d{2}:?\d{2}$/.test(s);
  return new Date(hasZone ? s : `${s}Z`);
}
