import { inject } from '@angular/core';
import { Router, type CanActivateFn, type CanMatchFn } from '@angular/router';
import { AUTH_ROUTES } from '@agentics/auth';

import type { WmsModuleCode } from './wms-me.model';
import { WmsSession } from './wms-session';

/**
 * WMS marshrut guard'lari — eski `wms-ui` bilan BIR XIL chaqiruv shakli:
 *
 * ```ts
 * { path: 'warehouse', canActivate: [moduleGuard('WAREHOUSE_RAW'), permissionGuard('warehouse.view')], ... }
 * { path: 'batches',   canActivate: [featureGuard('warehouse.batches')], ... }
 * ```
 *
 * Shu sababli ko'chirilgan `*.routes.ts` fayllari o'zgarishsiz qoladi.
 *
 * ⚠️ `@agentics/auth` da ham `permissionGuard` / `moduleGuard` bor, lekin ular
 * `route.data` dan o'qiydi va modulni `TenantStore` dan tekshiradi. Bu yerdagilar
 * FABRIKA (kod argument) va manba — `WmsSession` (`/api/me`). WMS marshrutlarida
 * FAQAT shu fayldagilar ishlatiladi; paketdan faqat `authGuard` va `tenantGuard`.
 *
 * Eski ilovadan FARQ: ruxsat yoki modul bo'lmasa foydalanuvchi endi jimgina
 * `/dashboard` ga qaytarilmaydi — sababi ko'rsatiladi: ruxsat yo'q → `/forbidden`,
 * tarifda yo'q → `/not-in-plan` (modul yoki feature nomi bilan).
 *
 * Guard'lar tartibi muhim: `[moduleGuard, permissionGuard]` — modul yuqori
 * qatlam, u o'chiq bo'lsa ruxsat haqida gapirish ma'nosiz. Angular birinchi
 * `true` bo'lmagan natijani massiv tartibida qo'llaydi.
 */
type WmsGuard = CanActivateFn & CanMatchFn;

/**
 * Kabinet (F9) va ilova qobig'i bir-birini almashtiradi.
 *
 * `portalGuard` — kabinet marshrutida: ilova foydalanuvchisi u yerga tushmasin.
 * `appShellGuard` — qobiqda: kabinet foydalanuvchisi bo'sh menyuli ilovani
 * ko'rmasin (uning WMS ruxsati ATAYLAB bo'sh, ya'ni har ekran 403 berardi).
 */
export function portalGuard(): WmsGuard {
  return () => {
    if (inject(WmsSession).isPortalUser()) {
      return true;
    }
    return inject(Router).createUrlTree(['/']);
  };
}

export function appShellGuard(): WmsGuard {
  return () => {
    if (!inject(WmsSession).isPortalUser()) {
      return true;
    }
    return inject(Router).createUrlTree(['/portal']);
  };
}

/** Kamida bitta ruxsat kodi talab qilinadi. */
export function permissionGuard(...codes: readonly string[]): WmsGuard {
  return () => {
    if (inject(WmsSession).canAny(...codes)) {
      return true;
    }
    return inject(Router).createUrlTree([inject(AUTH_ROUTES).forbidden]);
  };
}

/**
 * Identity mahsulot roli talab qilinadi (ruxsat kodi emas — `WmsSession.hasRole`).
 *
 * Hozircha faqat «Kirish hisoblari» (`/tenant/v1` faqat `admin` ga ochiq):
 * boshqa rol bilan sahifa ochilsa ham API 403 berardi, guard esa foydalanuvchini
 * darhol `/forbidden` ga yo'naltiradi — menyu bilan bir xil qoida.
 */
export function roleGuard(...roles: readonly string[]): WmsGuard {
  return () => {
    const session = inject(WmsSession);
    if (roles.some((role) => session.hasRole(role))) {
      return true;
    }
    return inject(Router).createUrlTree([inject(AUTH_ROUTES).forbidden]);
  };
}

/** Modul tenantda yoqilgan bo'lishi shart (11 kod — `WMS_MODULES`). */
export function moduleGuard(code: WmsModuleCode): WmsGuard {
  return () => {
    if (inject(WmsSession).isModuleEnabled(code)) {
      return true;
    }
    return inject(Router).createUrlTree([inject(AUTH_ROUTES).moduleDisabled], {
      queryParams: { module: code },
    });
  };
}

/** Feature (moduldan mayda dona) yoqilgan bo'lishi shart. */
export function featureGuard(code: string): WmsGuard {
  return () => {
    if (inject(WmsSession).isFeatureEnabled(code)) {
      return true;
    }
    return inject(Router).createUrlTree([inject(AUTH_ROUTES).moduleDisabled], {
      queryParams: { feature: code },
    });
  };
}
