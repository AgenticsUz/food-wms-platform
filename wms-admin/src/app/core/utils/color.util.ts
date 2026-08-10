/**
 * Brend rangi bilan ishlash. Mijozdan **bitta** rang so'raymiz — qolgan palitra
 * shundan hosil qilinadi (`wms-ui` tomonida ham shu qoida).
 */

const HEX = /^#?([0-9a-fA-F]{6})$/;

/** `#RRGGBB` ga keltiradi; noto'g'ri bo'lsa `null`. */
export function normalizeHex(value: string | null | undefined): string | null {
  const m = HEX.exec((value ?? '').trim());
  return m ? '#' + m[1].toLowerCase() : null;
}

export function isValidHex(value: string | null | undefined): boolean {
  return normalizeHex(value) !== null;
}

/** sRGB kanalini WCAG relativ yorqinligiga o'giradi. */
function channel(v: number): number {
  const s = v / 255;
  return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
}

function luminance(hex: string): number {
  const h = normalizeHex(hex)!.slice(1);
  const r = channel(parseInt(h.slice(0, 2), 16));
  const g = channel(parseInt(h.slice(2, 4), 16));
  const b = channel(parseInt(h.slice(4, 6), 16));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/**
 * Tanlangan rang ustidagi **oq** matn kontrasti (WCAG nisbati).
 * Tugmalar va sidebar'da matn oq bo'ladi, shuning uchun aynan shu nisbat muhim.
 */
export function contrastWithWhite(hex: string | null | undefined): number | null {
  if (!isValidHex(hex)) return null;
  const l = luminance(hex!);
  return Math.round(((1.05) / (l + 0.05)) * 100) / 100;
}

/** WCAG AA oddiy matn uchun minimal nisbat. */
export const AA_CONTRAST = 4.5;

/**
 * Rang oq matn bilan yetarlicha kontrast beradimi. `false` bo'lsa **bloklamaymiz**,
 * faqat ogohlantiramiz: aks holda mijoz och sariq tanlaydi va bu bizga qaytadi.
 */
export function hasReadableWhiteText(hex: string | null | undefined): boolean {
  const ratio = contrastWithWhite(hex);
  return ratio === null || ratio >= AA_CONTRAST;
}
