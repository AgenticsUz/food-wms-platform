import { Injectable, computed, inject, signal } from '@angular/core';
import { CurrentUserStore } from '@agentics/auth';

import type { WmsCurrentUser, WmsMe } from './wms-me.model';

/**
 * WMS sessiyasi — `GET /api/me` javobining WMS ko'rinishi.
 *
 * Manba BITTA: `@agentics/auth` ning `CurrentUserStore` i (uni `AuthService`
 * kirishda va sahifa yangilanganda to'ldiradi). Bu servis o'z holatini
 * SAQLAMAYDI — faqat o'sha foydalanuvchining `wms` bo'lagidan hosila signallar
 * beradi. Ikkinchi nusxa bo'lsa, u chiqishda tozalanmay qolishi mumkin edi.
 *
 * Eski `wms-ui` dagi uch servisning o'rnini bosadi:
 *  - `PermissionService.can()`  → `can()` / `canAny()`;
 *  - `TenantService.isModuleEnabled()` → `isModuleEnabled()`;
 *  - `FeatureService.isEnabled()` → `isFeatureEnabled()`.
 *
 * ⚠️ Bu tekshiruvlar faqat UX (menyu, tugma, guard). Haqiqiy chegara backendda:
 * `[RequirePermission]`, `[RequireModule]`, `[RequireFeature]`.
 */
@Injectable({ providedIn: 'root' })
export class WmsSession {
  private readonly users = inject(CurrentUserStore);

  /** Asl `/api/me` javobi; kirilmagan bo'lsa `null`. */
  readonly me = computed<WmsMe | null>(
    () => (this.users.user() as WmsCurrentUser | null)?.wms ?? null
  );

  readonly fullName = computed(() => this.me()?.fullName ?? '');
  readonly phone = computed(() => this.me()?.phone ?? null);
  readonly roles = computed(() => this.me()?.roles ?? []);
  /** Menyuda ko'rsatiladigan asosiy rol (`admin` > `manager` > …). */
  readonly primaryRole = computed(() => {
    const roles = this.roles();
    return ROLE_ORDER.find((role) => roles.includes(role)) ?? roles[0] ?? null;
  });

  readonly tenant = computed(() => this.me()?.tenant ?? null);
  readonly subscription = computed(() => this.me()?.subscription ?? null);

  readonly modules = computed<ReadonlySet<string>>(() => new Set(this.me()?.modules ?? []));
  readonly features = computed<ReadonlySet<string>>(() => new Set(this.me()?.features ?? []));

  /**
   * Sessiya davomida kelgan 402 ning kodi (`WmsErrorNotifier` yozadi). `/me`
   * dagi kod eskirgan bo'lishi mumkin — obuna so'rovlar orasida to'xtatilgan.
   */
  private readonly blockCodeState = signal<string | null>(null);

  /** Blok sababi: avval sessiyadagi 402, bo'lmasa `/me` dagi `subscription.code`. */
  readonly blockCode = computed(() => {
    const live = this.blockCodeState();
    if (live !== null) {
      return live;
    }
    const subscription = this.subscription();
    return subscription !== null && !subscription.allowed ? subscription.code : null;
  });

  /** Ruxsat bormi. Platforma admini uchun har doim `true` (paket qoidasi). */
  can(code: string): boolean {
    return this.users.hasPermission(code);
  }

  /** Kamida bittasi bo'lsa yetarli. */
  canAny(...codes: readonly string[]): boolean {
    return codes.length === 0 || this.users.hasAnyPermission(...codes);
  }

  /** Modul tenantda yoqilganmi (D6 — manba Identity). */
  isModuleEnabled(code: string): boolean {
    return this.modules().has(code);
  }

  /**
   * Feature yoqilganmi.
   *
   * ⚠️ Eski `FeatureService` dan FARQ: u ro'yxat BO'SH bo'lsa hammasini ochiq
   * deb hisoblardi (backend feature qatlamini hali yubormagan davr uchun). Endi
   * `/api/me` shartnomasida `features` DOIM bor, ya'ni bo'sh ro'yxat — «hech
   * narsa yoqilmagan». Platforma qoidasi: bo'sh ro'yxat = hech kimga ruxsat yo'q
   * (fail-closed). Backend ro'yxatni bermay qo'ysa menyu bo'sh ko'rinadi — bu
   * ko'rinadigan nosozlik, jimgina hammasini ochib yuborish emas.
   */
  isFeatureEnabled(code: string): boolean {
    return this.features().has(code);
  }

  markBlocked(code: string | null): void {
    this.blockCodeState.set(code);
  }

  clearBlock(): void {
    this.blockCodeState.set(null);
  }
}

/** Identity `wms` mahsuloti rollari, kattasidan boshlab (D5). */
const ROLE_ORDER: readonly string[] = ['admin', 'manager', 'employee', 'viewer'];
