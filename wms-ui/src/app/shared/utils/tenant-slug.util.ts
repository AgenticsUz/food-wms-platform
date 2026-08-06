import { environment } from '../../../environments/environment';

/**
 * Qaysi tenantga kirilayotganini aniqlash.
 *
 * Backend `POST /api/auth/login` da `tenantSlug` ni majburiy so'raydi: telefon raqam
 * global emas, faqat tenant ichida noyob. Ilgari bu qiymat `environment.tenantSlug` da
 * qattiq yozilgan edi, ya'ni bitta build faqat bitta mijozga xizmat qilardi va admin
 * panelda yaratilgan yangi tenant umuman kira olmasdi.
 *
 * Endi tartib shunday:
 *   1. Subdomen — `sinov.wms.uz` → `sinov` (R20 rejasi shu holda o'zi ishlaydi).
 *   2. Oxirgi muvaffaqiyatli kirishda eslab qolingan qiymat.
 *   3. `environment.tenantSlug` — standart.
 *
 * Subdomen bo'lmasa (localhost, yalang'och domen) login formasi tashkilot kodini
 * so'raydi; subdomen bo'lsa maydon ko'rsatilmaydi.
 */

const STORAGE_KEY = 'tenantSlug';

/** Tenant sifatida qaralmaydigan subdomenlar. */
const RESERVED_SUBDOMAINS = new Set(['www', 'app', 'admin', 'api', 'localhost']);

const IPV4 = /^\d{1,3}(\.\d{1,3}){3}$/;

/** `sinov.wms.uz` → `sinov`. Subdomen bo'lmasa yoki xizmat nomi bo'lsa — null. */
export function slugFromHost(host: string = window.location.hostname): string | null {
  if (!host || host === 'localhost' || IPV4.test(host)) return null;
  const parts = host.split('.');
  // `wms.uz` — subdomen yo'q; kamida `sub.domen.zona` kerak.
  if (parts.length < 3) return null;
  const sub = parts[0].trim().toLowerCase();
  return sub && !RESERVED_SUBDOMAINS.has(sub) ? sub : null;
}

/** Subdomen slug bersa, foydalanuvchidan so'ramaymiz. */
export function isSlugFromHost(): boolean {
  return slugFromHost() !== null;
}

export function storedSlug(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY)?.trim() || null;
  } catch {
    return null;
  }
}

/** Login formasi shu qiymatdan boshlanadi. */
export function resolveTenantSlug(): string {
  return slugFromHost() ?? storedSlug() ?? environment.tenantSlug;
}

/** Muvaffaqiyatli kirishdan keyin — keyingi safar qayta yozmasin. */
export function rememberTenantSlug(slug: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, normalizeSlug(slug));
  } catch {
    // localStorage yopiq bo'lsa ham kirish ishlashi kerak
  }
}

/** Backend slug'ni kichik harfda saqlaydi; foydalanuvchi katta harf yozishi mumkin. */
export function normalizeSlug(slug: string): string {
  return slug.trim().toLowerCase();
}
