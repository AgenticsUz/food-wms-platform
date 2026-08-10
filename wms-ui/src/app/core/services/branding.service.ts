import { Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment';

/** `/api/auth/login`, `/api/subscription/me` va `/api/public/branding` bir xil obyekt qaytaradi. */
export interface Branding {
  logoUrl: string | null;
  logoSquareUrl: string | null;
  brandColor: string | null;
}

/**
 * Saqlangan nusxa. Brend rangi CSS o'zgaruvchilari bilan qo'llanadi, ya'ni u faqat
 * Angular ishga tushgandan keyin ta'sir qiladi. Agar qiymatni har safar serverdan
 * kutsak, mijoz avval bizning yashil rangimizni, so'ng o'zinikini ko'radi — bu
 * "sayt buzilgan" degan taassurot beradi. Shuning uchun oxirgi ma'lum brend
 * `localStorage` da turadi va ilova ochilishida darhol qo'llanadi.
 */
const STORAGE_KEY = 'branding';

const HEX = /^#?([0-9a-fA-F]{6})$/;

/** PrimeNG Aura shu o'zgaruvchilarni o'qiydi; `documentElement` inline uslubi `:root` dan ustun. */
const PRIMARY_VARS = [
  '--p-primary-50', '--p-primary-100', '--p-primary-200', '--p-primary-300',
  '--p-primary-400', '--p-primary-500', '--p-primary-600', '--p-primary-700',
  '--p-primary-color', '--p-primary-hover-color', '--p-primary-active-color',
  '--p-button-primary-background', '--p-button-primary-hover-background',
  '--p-button-primary-border-color',
  '--brand-primary', '--brand-primary-soft', '--brand-primary-strong'
];

interface StoredBranding extends Branding {
  tenantName: string | null;
}

@Injectable({ providedIn: 'root' })
export class BrandingService {
  logoUrl = signal<string | null>(null);
  logoSquareUrl = signal<string | null>(null);
  brandColor = signal<string | null>(null);
  tenantName = signal<string | null>(null);

  /** Ilova ishga tushganda — serverdan javob kelishini kutmasdan. */
  restore(): void {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return;
    try {
      this.set(JSON.parse(raw) as StoredBranding);
    } catch {
      localStorage.removeItem(STORAGE_KEY);
    }
  }

  /** Login javobi yoki `subscription/me` dan kelgan brendni qo'llaydi va saqlaydi. */
  apply(branding: Branding | null | undefined, tenantName?: string | null): void {
    const value: StoredBranding = {
      logoUrl: branding?.logoUrl ?? null,
      logoSquareUrl: branding?.logoSquareUrl ?? null,
      brandColor: normalizeHex(branding?.brandColor),
      tenantName: tenantName ?? this.tenantName()
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    this.set(value);
  }

  /**
   * Chiqishda tozalanadi. Bitta kompyuterdan ikki mijoz kirsa, ikkinchisi birinchisining
   * logotipi va rangini ko'rmasligi kerak — bu shunchaki chiroyli emas, mijozga boshqa
   * kompaniyaning brendi ko'rsatilgani.
   */
  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.set({ logoUrl: null, logoSquareUrl: null, brandColor: null, tenantName: null });
  }

  /** Nisbiy `/uploads/...` manzilini to'liq URL'ga aylantiradi. */
  logoSrc(url: string | null): string | null {
    if (!url) return null;
    return url.startsWith('http') ? url : apiOrigin() + url;
  }

  private set(value: StoredBranding): void {
    this.logoUrl.set(value.logoUrl);
    this.logoSquareUrl.set(value.logoSquareUrl);
    this.brandColor.set(value.brandColor);
    this.tenantName.set(value.tenantName ?? null);

    applyPalette(value.brandColor);
    applyFavicon(this.logoSrc(value.logoSquareUrl) ?? this.logoSrc(value.logoUrl));
    applyTitle(value.tenantName);
  }
}

/* ── Palitra ──────────────────────────────────────────────────────────────── */

function normalizeHex(value: string | null | undefined): string | null {
  const m = HEX.exec((value ?? '').trim());
  return m ? '#' + m[1].toLowerCase() : null;
}

function rgb(hex: string): [number, number, number] {
  const h = hex.slice(1);
  return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)];
}

function toHex([r, g, b]: [number, number, number]): string {
  const p = (n: number) => Math.max(0, Math.min(255, Math.round(n))).toString(16).padStart(2, '0');
  return `#${p(r)}${p(g)}${p(b)}`;
}

/** `ratio` = 0 → asl rang, 1 → maqsad rang (oq yoki qora). */
function mix(base: [number, number, number], target: number, ratio: number): [number, number, number] {
  return [
    base[0] + (target - base[0]) * ratio,
    base[1] + (target - base[1]) * ratio,
    base[2] + (target - base[2]) * ratio
  ];
}

/**
 * Bitta rangdan butun palitra. Mijozdan besh xil rang so'rash — javobsiz qoladigan
 * savol; u bitta "firma rangi"ni biladi, qolganini biz hisoblaymiz.
 */
function applyPalette(color: string | null): void {
  const root = document.documentElement;

  if (!color) {
    for (const v of PRIMARY_VARS) root.style.removeProperty(v);
    return;
  }

  const base = rgb(color);
  const shades: Record<string, string> = {
    '--p-primary-50': toHex(mix(base, 255, 0.92)),
    '--p-primary-100': toHex(mix(base, 255, 0.84)),
    '--p-primary-200': toHex(mix(base, 255, 0.66)),
    '--p-primary-300': toHex(mix(base, 255, 0.46)),
    '--p-primary-400': toHex(mix(base, 255, 0.22)),
    '--p-primary-500': color,
    '--p-primary-600': toHex(mix(base, 0, 0.18)),
    '--p-primary-700': toHex(mix(base, 0, 0.34)),
    '--p-primary-color': color,
    '--p-primary-hover-color': toHex(mix(base, 0, 0.18)),
    '--p-primary-active-color': toHex(mix(base, 0, 0.34)),
    '--p-button-primary-background': color,
    '--p-button-primary-hover-background': toHex(mix(base, 0, 0.18)),
    '--p-button-primary-border-color': color,
    '--brand-primary': color,
    '--brand-primary-soft': toHex(mix(base, 255, 0.84)),
    '--brand-primary-strong': toHex(mix(base, 0, 0.34))
  };

  for (const [name, value] of Object.entries(shades)) root.style.setProperty(name, value);
}

/* ── Favicon va sarlavha ──────────────────────────────────────────────────── */

const DEFAULT_FAVICON = 'favicon.ico';
const DEFAULT_TITLE = 'WMS Platform';

function applyFavicon(href: string | null): void {
  let link = document.querySelector<HTMLLinkElement>('link[rel~="icon"]');
  if (!link) {
    link = document.createElement('link');
    link.rel = 'icon';
    document.head.appendChild(link);
  }
  const next = href ?? DEFAULT_FAVICON;
  // `type` ni olib tashlaymiz: `image/x-icon` deb belgilangan PNG ba'zi brauzerlarda chizilmaydi.
  if (href) link.removeAttribute('type'); else link.type = 'image/x-icon';
  if (link.getAttribute('href') !== next) link.setAttribute('href', next);
}

function applyTitle(tenantName: string | null): void {
  document.title = tenantName ? `${tenantName} — WMS` : DEFAULT_TITLE;
}

/**
 * Logolar `/uploads/...` da, `/api` OSTIDA EMAS. `apiUrl` nisbiy (`/api`) bo'lganda
 * ular shu domendan olinadi; absolyut bo'lsa — o'sha xostdan.
 */
function apiOrigin(): string {
  const base = environment.apiUrl;
  if (!base.startsWith('http')) return '';
  try { return new URL(base).origin; } catch { return ''; }
}
