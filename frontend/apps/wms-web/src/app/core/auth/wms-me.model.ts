import type { CurrentUser } from '@agentics/auth';

/**
 * `GET /api/me` ning `data` qismi — backend AYNAN shu shaklni beradi (W2
 * shartnomasi, PLATFORMA-TZ §5.2). Barcha id'lar — Guid satr.
 *
 * ⚠️ Bu shakl platforma paketining `CurrentUser`/`TenantInfo` shakli bilan bir xil
 * EMAS (`sub` ↔ `id`, obuna holati `tenant` ichida emas, alohida). Moslash
 * `me-adapter.ts` da, BITTA joyda — backend shartnomasini paketga moslab
 * o'zgartirish WMS backend agentlarining ishini buzardi.
 */
export interface WmsMe {
  /** Identity `sub`. */
  readonly sub: string;
  /** WMS `user_profile.id` (JIT nusxa); hali yaratilmagan bo'lsa `null`. */
  readonly profileId: string | null;
  readonly fullName: string;
  readonly phone: string | null;
  readonly isPlatformAdmin: boolean;
  readonly tenant: WmsTenant | null;
  /** Identity `wms` mahsuloti rollari: `admin`, `manager`, `employee`, `viewer`. */
  readonly roles: readonly string[];
  /** WMS ruxsat kodlari (`warehouse.view`, …) — `WmsRoleMap` + Rollar ekrani. */
  readonly permissions: readonly string[];
  /** Yoqilgan modullar (D6 — manba Identity `tenant_product.modules`). */
  readonly modules: readonly string[];
  /** Yoqilgan feature kodlari (plan + tenant override). */
  readonly features: readonly string[];
  readonly subscription: WmsSubscription | null;
}

export interface WmsTenant {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  /** Keng logo — `/uploads/...` (nisbiy) yoki absolyut manzil. */
  readonly logoUrl: string | null;
  /** Kvadrat logo — yig'ilgan menyu va favicon uchun. */
  readonly logoSquareUrl: string | null;
  /** `#RRGGBB` — bitta firma rangi, palitra frontendda hisoblanadi (D14). */
  readonly brandColor: string | null;
}

export type WmsSubscriptionStatus = 'Trial' | 'Active' | 'Suspended';

export interface WmsSubscription {
  readonly status: WmsSubscriptionStatus;
  /** Server qarori: ishlash mumkinmi. Holatdan ustun — sabab `code` da. */
  readonly allowed: boolean;
  /** Blok sababi: `trial_expired`, `payment_expired`, `suspended_*`, `tenant_inactive`… */
  readonly code: string | null;
  /** Server matni (tarjima qilingan). */
  readonly message: string | null;
  /** Administrator mijozga yozgan ochiq matn — `message` dan ustun. */
  readonly publicMessage: string | null;
  readonly planCode: string | null;
  readonly planName: string | null;
  readonly trialEndsAt: string | null;
  readonly paidUntil: string | null;
  readonly trialDaysLeft: number | null;
  readonly paidDaysLeft: number | null;
}

/**
 * `CurrentUserStore` da saqlanadigan foydalanuvchi: platforma ko'rinishi +
 * asl WMS javobi (`wms`). Paket o'z maydonlarini o'qiydi, WMS kodi esa
 * `wms` ni — ikkalasi bitta manbadan, bitta `/me` so'rovidan.
 */
export interface WmsCurrentUser extends CurrentUser {
  readonly wms: WmsMe;
}

/** 11 ta modul kodi (PLATFORMA-TZ §7·F6.1, backend `ModuleCodes`). */
export const WMS_MODULES = [
  'WAREHOUSE_RAW',
  'PRODUCTION',
  'WAREHOUSE_FINISHED',
  'TRANSFERS',
  'FINANCE',
  'KPI',
  'SUPPLIERS',
  'CLIENTS',
  'QUALITY',
  'AGENTS',
  'DELIVERY',
] as const;

export type WmsModuleCode = (typeof WMS_MODULES)[number];
