/**
 * Bitta firma rangidan butun palitra (eski `wms-ui/core/services/branding.service.ts`).
 *
 * Mijozdan besh xil rang so'rash — javobsiz qoladigan savol; u bitta «firma
 * rangi» ni biladi, qolganini biz hisoblaymiz: och soyalar oq bilan, to'q
 * soyalar qora bilan aralashtiriladi.
 */

const HEX = /^#?([0-9a-fA-F]{6})$/;

type Rgb = readonly [number, number, number];

/**
 * PrimeNG Aura shu o'zgaruvchilarni o'qiydi; `documentElement` ning inline
 * uslubi `:root` dan ustun, ya'ni `styles.css` dagi pistachio standarti
 * o'chirilmasdan ustiga yoziladi. `--brand-*` — ilovaning o'z sinflari uchun.
 */
export const PRIMARY_VARS: readonly string[] = [
  '--p-primary-50',
  '--p-primary-100',
  '--p-primary-200',
  '--p-primary-300',
  '--p-primary-400',
  '--p-primary-500',
  '--p-primary-600',
  '--p-primary-700',
  '--p-primary-color',
  '--p-primary-hover-color',
  '--p-primary-active-color',
  '--p-button-primary-background',
  '--p-button-primary-hover-background',
  '--p-button-primary-border-color',
  '--brand-primary',
  '--brand-primary-soft',
  '--brand-primary-strong',
];

/** `#RRGGBB` ga keltiradi; yaroqsiz bo'lsa `null` (standart palitra qoladi). */
export function normalizeHex(value: string | null | undefined): string | null {
  const match = HEX.exec((value ?? '').trim());
  return match ? `#${match[1].toLowerCase()}` : null;
}

function rgb(hex: string): Rgb {
  const h = hex.slice(1);
  return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)];
}

function toHex([r, g, b]: Rgb): string {
  const p = (n: number): string =>
    Math.max(0, Math.min(255, Math.round(n)))
      .toString(16)
      .padStart(2, '0');
  return `#${p(r)}${p(g)}${p(b)}`;
}

/** `ratio` = 0 → asl rang, 1 → maqsad rang (255 — oq, 0 — qora). */
function mix(base: Rgb, target: number, ratio: number): Rgb {
  return [
    base[0] + (target - base[0]) * ratio,
    base[1] + (target - base[1]) * ratio,
    base[2] + (target - base[2]) * ratio,
  ];
}

/** Normallashtirilgan `#rrggbb` dan CSS o'zgaruvchilari xaritasi. */
export function buildPalette(color: string): Readonly<Record<string, string>> {
  const base = rgb(color);
  const hover = toHex(mix(base, 0, 0.18));
  const active = toHex(mix(base, 0, 0.34));
  return {
    '--p-primary-50': toHex(mix(base, 255, 0.92)),
    '--p-primary-100': toHex(mix(base, 255, 0.84)),
    '--p-primary-200': toHex(mix(base, 255, 0.66)),
    '--p-primary-300': toHex(mix(base, 255, 0.46)),
    '--p-primary-400': toHex(mix(base, 255, 0.22)),
    '--p-primary-500': color,
    '--p-primary-600': hover,
    '--p-primary-700': active,
    '--p-primary-color': color,
    '--p-primary-hover-color': hover,
    '--p-primary-active-color': active,
    '--p-button-primary-background': color,
    '--p-button-primary-hover-background': hover,
    '--p-button-primary-border-color': color,
    '--brand-primary': color,
    '--brand-primary-soft': toHex(mix(base, 255, 0.84)),
    '--brand-primary-strong': active,
  };
}
