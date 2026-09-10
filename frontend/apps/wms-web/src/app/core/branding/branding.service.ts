import { DOCUMENT } from '@angular/common';
import {
  Injectable,
  computed,
  effect,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
  provideEnvironmentInitializer,
  signal,
  type EnvironmentProviders,
} from '@angular/core';
import { APP_STORAGE_PREFIX, ConfigService } from '@agentics/config';

import type { WmsTenant } from '../auth/wms-me.model';
import { WmsSession } from '../auth/wms-session';
import { buildPalette, normalizeHex, PRIMARY_VARS } from './palette';

interface StoredBranding {
  readonly logoUrl: string | null;
  readonly logoSquareUrl: string | null;
  readonly brandColor: string | null;
  readonly tenantName: string | null;
}

const EMPTY: StoredBranding = { logoUrl: null, logoSquareUrl: null, brandColor: null, tenantName: null };
const DEFAULT_TITLE = 'Agentics WMS';
const DEFAULT_FAVICON = 'favicon.ico';

/**
 * Tenant brendi: logo (keng/kvadrat), firma rangi → palitra, favicon, sarlavha (D14).
 *
 * Manba — `/api/me` ning `tenant` bo'lagi (`GET /api/public/branding` O'CHDI:
 * login sahifasi endi Identity'da va u neytral).
 *
 * ⚠️ Oxirgi ma'lum brend `localStorage` da (`wms.branding`) va ilova
 * ochilishida DARHOL qo'llanadi. Aks holda mijoz avval bizning pistachio
 * rangimizni, `/me` kelgach o'zinikini ko'rardi — sahifa «sakraydi».
 * Chiqishda yozuv `AuthService` tomonidan o'chiriladi (u `wms.*` prefiksli
 * hamma kalitni tozalaydi): bitta kompyuterdan ikki mijoz kirsa, ikkinchisi
 * birinchisining logotipini ko'rmasligi kerak.
 */
@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly document = inject(DOCUMENT);
  private readonly config = inject(ConfigService);
  private readonly storageKey = `${inject(APP_STORAGE_PREFIX)}branding`;

  private readonly state = signal<StoredBranding>(EMPTY);

  readonly tenantName = computed(() => this.state().tenantName);
  readonly brandColor = computed(() => this.state().brandColor);
  /** Keng logo (yoyilgan menyu). */
  readonly wideLogoSrc = computed(() => this.logoSrc(this.state().logoUrl));
  /** Kvadrat logo, bo'lmasa keng (sig'diriladi, kesilmaydi). */
  readonly squareLogoSrc = computed(() =>
    this.logoSrc(this.state().logoSquareUrl ?? this.state().logoUrl)
  );

  /** Ilova ochilishida — server javobini kutmasdan. */
  restore(): void {
    try {
      const raw = globalThis.localStorage?.getItem(this.storageKey);
      if (raw) {
        this.set(JSON.parse(raw) as StoredBranding);
      }
    } catch {
      globalThis.localStorage?.removeItem(this.storageKey);
    }
  }

  /** `/api/me` dan kelgan brendni qo'llaydi va saqlaydi. */
  apply(tenant: WmsTenant | null): void {
    const value: StoredBranding = {
      logoUrl: tenant?.logoUrl ?? null,
      logoSquareUrl: tenant?.logoSquareUrl ?? null,
      brandColor: normalizeHex(tenant?.brandColor),
      tenantName: tenant?.name ?? null,
    };
    try {
      globalThis.localStorage?.setItem(this.storageKey, JSON.stringify(value));
    } catch {
      // Maxfiylik rejimi — brend baribir qo'llanadi, faqat eslab qolinmaydi.
    }
    this.set(value);
  }

  /**
   * Nisbiy `/uploads/...` → to'liq URL. Logolar `/api` OSTIDA EMAS; `apiUrl`
   * nisbiy bo'lsa shu origin, absolyut bo'lsa o'sha xost.
   */
  logoSrc(url: string | null): string | null {
    if (!url) {
      return null;
    }
    if (/^https?:\/\//i.test(url)) {
      return url;
    }
    const base = this.config.apiUrl();
    if (!/^https?:\/\//i.test(base)) {
      return url;
    }
    try {
      return new URL(base).origin + url;
    } catch {
      return url;
    }
  }

  private set(value: StoredBranding): void {
    this.state.set(value);
    this.applyPalette(value.brandColor);
    this.applyFavicon(this.logoSrc(value.logoSquareUrl) ?? this.logoSrc(value.logoUrl));
    this.document.title = value.tenantName ? `${value.tenantName} — WMS` : DEFAULT_TITLE;
  }

  private applyPalette(color: string | null): void {
    const root = this.document.documentElement;
    if (color === null) {
      for (const name of PRIMARY_VARS) {
        root.style.removeProperty(name);
      }
      return;
    }
    for (const [name, value] of Object.entries(buildPalette(color))) {
      root.style.setProperty(name, value);
    }
  }

  private applyFavicon(href: string | null): void {
    let link = this.document.querySelector<HTMLLinkElement>('link[rel~="icon"]');
    if (link === null) {
      link = this.document.createElement('link');
      link.rel = 'icon';
      this.document.head.appendChild(link);
    }
    const next = href ?? DEFAULT_FAVICON;
    // `type` olib tashlanadi: `image/x-icon` deb belgilangan PNG ba'zi brauzerlarda chizilmaydi.
    if (href) {
      link.removeAttribute('type');
    } else {
      link.type = 'image/x-icon';
    }
    if (link.getAttribute('href') !== next) {
      link.setAttribute('href', next);
    }
  }
}

/**
 * Brendni ulaydi: ochilishda saqlangan nusxa, keyin `/api/me` ga ergashish.
 *
 * `effect` ildiz injektorida — foydalanuvchi almashsa (chiqish/kirish) yoki
 * `/me` qayta o'qilsa brend o'zi yangilanadi, hech bir komponent buni eslab
 * qolishi shart emas.
 */
export function provideBranding(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => inject(BrandingService).restore()),
    provideEnvironmentInitializer(() => {
      const session = inject(WmsSession);
      const branding = inject(BrandingService);
      effect(() => {
        const me = session.me();
        if (me !== null) {
          branding.apply(me.tenant);
        }
      });
    }),
  ]);
}
