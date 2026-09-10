import type { HttpErrorResponse } from '@angular/common/http';
import { isAppError, toAppError, type AppError, type AppErrorKind } from '@agentics/http';

import { isApiResponse, type ApiResponse } from './api-response.model';

/**
 * WMS API xatosi — platformaning `AppError` i + WMS konvertining o'zi.
 *
 * ⚠️ NEGA `AppError` KENGAYTMASI. Paket kodi xatoni `AppError` sifatida kutadi:
 * `authInterceptor` 401 ni `status` bo'yicha taniydi, `AuthService.refresh()`
 * tarmoq uzilishini `kind === 'network'` bo'yicha. Shu shartnomani buzmaslik
 * uchun WMS xatosi ham `status` + `kind` ga ega.
 *
 * ⚠️ NEGA `error` MAYDONI. Eski `wms-ui` ekranlari xato matnini
 * `err.error?.message` dan o'qiydi (`HttpErrorResponse` shakli). Maydon shu
 * nomda qoldirilgani uchun ko'chirilgan ekran o'zgarishsiz ishlaydi.
 */
export interface WmsApiError extends AppError {
  /** Server matni — `Accept-Language` bo'yicha tarjima qilingan. */
  readonly message: string | null;
  /** WMS mashina kodi: `trial_expired`, `module_disabled:PRODUCTION`, `limit_users`… */
  readonly wmsCode: string | null;
  /** Asl konvert (`HttpErrorResponse.error` bilan bir xil nom — ko'chirish uchun). */
  readonly error: ApiResponse<unknown> | null;
}

/** 402 dagi obuna bloki kodlari — backend `SubscriptionPolicy` bilan bir xil. */
export const SUBSCRIPTION_BLOCK_CODES: readonly string[] = [
  'tenant_missing',
  'tenant_inactive',
  'trial_expired',
  'payment_expired',
  'suspended_nonpayment',
  'suspended_request',
  'suspended_technical',
  'suspended_violation',
  'suspended_other',
];

/** Blok sababi → tarjima kaliti (eski `subscription.model.ts` xaritasi). */
const BLOCKED_KEYS: Readonly<Record<string, string>> = {
  tenant_missing: 'errors.tenantInactive',
  tenant_inactive: 'errors.tenantInactive',
  trial_expired: 'errors.trialExpired',
  payment_expired: 'errors.paymentExpired',
  suspended_nonpayment: 'errors.suspendedNonpayment',
  suspended_request: 'errors.suspendedRequest',
  suspended_technical: 'errors.suspendedTechnical',
  suspended_violation: 'errors.suspendedViolation',
  suspended_other: 'errors.suspendedOther',
};

/** 402 dagi tarif limiti kodlari — BLOK EMAS, foydalanuvchi sahifada qoladi. */
export const LIMIT_KEYS: Readonly<Record<string, string>> = {
  limit_users: 'errors.limitUsers',
  limit_warehouses: 'errors.limitWarehouses',
  limit_transfers: 'errors.limitTransfers',
};

/** Noma'lum kod ham blok sifatida ko'rsatiladi — mijoz sababsiz qolmasin. */
export function blockedReasonKey(code: string | null | undefined): string {
  return (code !== null && code !== undefined && BLOCKED_KEYS[code]) || 'errors.subscriptionSuspended';
}

export function isLimitCode(code: string | null | undefined): boolean {
  return code !== null && code !== undefined && code in LIMIT_KEYS;
}

/** `module_disabled:PRODUCTION` → `PRODUCTION`; boshqa kod → `null`. */
export function disabledModuleOf(code: string | null | undefined): string | null {
  return code?.startsWith('module_disabled:') ? code.slice('module_disabled:'.length) : null;
}

/** `feature_disabled:warehouse.batches` → `warehouse.batches`; boshqa kod → `null`. */
export function disabledFeatureOf(code: string | null | undefined): string | null {
  return code?.startsWith('feature_disabled:') ? code.slice('feature_disabled:'.length) : null;
}

function kindOf(status: number, code: string | null): AppErrorKind {
  if (status === 0) return 'network';
  if (status >= 500) return 'server';
  switch (status) {
    case 400:
      return 'business';
    case 401:
      return 'unauthorized';
    case 402:
      // Limit — oddiy biznes-xato (toast); qolgan HAMMA 402 — obuna bloki,
      // kod noma'lum bo'lsa ham (402 boshqa hech narsa uchun ishlatilmaydi).
      return isLimitCode(code) ? 'business' : 'subscription';
    case 403:
      return disabledModuleOf(code) !== null || disabledFeatureOf(code) !== null
        ? 'module-disabled'
        : 'forbidden';
    case 404:
      return 'not-found';
    case 409:
      return 'conflict';
    case 422:
      return 'business';
    default:
      return 'unknown';
  }
}

function titleOf(kind: AppErrorKind, code: string | null): string {
  switch (kind) {
    case 'subscription':
      return blockedReasonKey(code);
    case 'module-disabled':
      return disabledFeatureOf(code) !== null ? 'errors.featureDisabled' : 'errors.moduleDisabled';
    case 'forbidden':
      return 'errors.accessDenied';
    case 'business':
      return code !== null && isLimitCode(code) ? LIMIT_KEYS[code] : 'errors.business';
    case 'network':
      return 'errors.offline';
    default:
      return `errors.${kind === 'not-found' ? 'notFound' : kind}`;
  }
}

/**
 * `HttpErrorResponse` → `WmsApiError`.
 *
 * Tana WMS konverti bo'lmasa (masalan ASP.NET'ning avtomatik 400
 * `ValidationProblemDetails` i yoki proksining HTML sahifasi) platformaning
 * `toAppError` i ishlaydi — u RFC 9457 ni o'qiydi, `validation` va maydon
 * xatolarini ham to'g'ri ajratadi.
 */
export function toWmsApiError(response: HttpErrorResponse, correlationId?: string): WmsApiError {
  const body: unknown = response.error;
  if (!isApiResponse(body)) {
    const base = toAppError(response, correlationId);
    return { ...base, message: base.detail ?? null, wmsCode: base.code ?? null, error: null };
  }

  const code = body.code ?? null;
  const kind = kindOf(response.status, code);
  return {
    status: response.status,
    kind,
    code: code ?? undefined,
    title: titleOf(kind, code),
    detail: body.message ?? undefined,
    correlationId,
    url: response.url ?? undefined,
    message: body.message ?? null,
    wmsCode: code,
    error: body,
  };
}

export function isWmsApiError(value: unknown): value is WmsApiError {
  return isAppError(value) && 'wmsCode' in value;
}
