/**
 * Obuna modeli — backend `DTOs/Subscription/SubscriptionDtos.cs` va
 * `DTOs/Plans/PlanDtos.cs` bilan bir xil.
 *
 * F6: `branding` O'CHDI (brend `/api/me` bilan keladi, D14); `PlanDto.moduleCodes`
 * O'CHDI (modul Identity obunasida, D6) — plan endi feature to'plamini beradi.
 * Eski ilovadagi «eski javob shakli» normalizatsiyasi ham kerak emas.
 */

/**
 * Bitta limit. Foiz va «limitga yaqin» bahosi BACKENDda hisoblanadi
 * (`Subscription:LimitWarnPercent`) — bu yerda qayta hisoblanmaydi. Plansiz
 * tenantda `usagePercent` va `isNearLimit` — `null`.
 */
export interface LimitDetail {
  readonly max: number;
  readonly current: number;
  readonly usagePercent: number | null;
  readonly isNearLimit: boolean | null;
}

export interface SubscriptionLimits {
  readonly maxUsers: number;
  readonly currentUsers: number;
  readonly maxWarehouses: number;
  readonly currentWarehouses: number;
  readonly maxTransfersPerMonth: number;
  readonly currentTransfersThisMonth: number;
  readonly users: LimitDetail;
  readonly warehouses: LimitDetail;
  readonly transfers: LimitDetail;
}

export type LimitKind = 'users' | 'warehouses' | 'transfers';

export interface SubscriptionInfo {
  readonly tenantId: string;
  readonly tenantName: string;
  /** `null` — plan biriktirilmagan (cheklovsiz). */
  readonly planName: string | null;
  readonly planCode: string | null;
  readonly planPrice: number;
  /** `"Trial" | "Active" | "Suspended"` — enum nomi. */
  readonly status: string;

  readonly trialEndsAt: string | null;
  readonly daysUntilTrialEnd: number | null;
  readonly paidUntil: string | null;
  readonly daysUntilPaidEnd: number | null;
  readonly paymentGraceDays: number;

  readonly isBlocked: boolean;
  /** 402 dagi `code` bilan bir xil lug'at. */
  readonly blockedReason: string | null;
  /** Operator yozgan matn — standart tarjimadan ustun. */
  readonly blockedMessage: string | null;
  readonly suspendedUntil: string | null;

  /** Server qarori: trial yoki to'langan davr `warnBeforeDays` ichida tugaydi. */
  readonly isExpiringSoon: boolean;
  readonly warnBeforeDays: number;
  readonly limitWarnPercent: number;

  readonly limits: SubscriptionLimits;
  readonly enabledModules: readonly string[];
  readonly enabledFeatures: readonly string[];

  readonly supportPhone: string | null;
  readonly supportEmail: string | null;
}

/** `GET subscription/plans`. */
export interface SubscriptionPlan {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly price: number;
  readonly isActive: boolean;
  readonly featureCodes: readonly string[];
  readonly maxUsers: number;
  readonly maxWarehouses: number;
  readonly maxTransfersPerMonth: number;
  readonly tenantCount: number;
  readonly trialDays: number;
  readonly isDefault: boolean;
}

/** Sahifadagi bitta limit qatori — faqat ko'rsatish shakli, hisob yo'q. */
export interface LimitRow {
  readonly key: LimitKind;
  readonly used: number;
  readonly limit: number;
  readonly isUnlimited: boolean;
  readonly percent: number;
  readonly isExceeded: boolean;
  readonly isNearLimit: boolean;
}

export function toLimitRows(limits: SubscriptionLimits | null): LimitRow[] {
  if (!limits) return [];
  const rows: readonly { key: LimitKind; detail: LimitDetail }[] = [
    { key: 'users', detail: limits.users },
    { key: 'warehouses', detail: limits.warehouses },
    { key: 'transfers', detail: limits.transfers },
  ];
  return rows.map(({ key, detail }) => {
    const isUnlimited = !detail.max || detail.max <= 0;
    return {
      key,
      used: detail.current,
      limit: detail.max,
      isUnlimited,
      percent: detail.usagePercent ?? 0,
      isExceeded: !isUnlimited && detail.current >= detail.max,
      isNearLimit: detail.isNearLimit === true,
    };
  });
}

/** Blok sababi → tarjima kaliti (kalitlar `errors.*` da). */
const BLOCKED_KEYS: Readonly<Record<string, string>> = {
  trial_expired: 'errors.trialExpired',
  payment_expired: 'errors.paymentExpired',
  suspended_nonpayment: 'errors.suspendedNonpayment',
  suspended_request: 'errors.suspendedRequest',
  suspended_technical: 'errors.suspendedTechnical',
  suspended_violation: 'errors.suspendedViolation',
  suspended_other: 'errors.suspendedOther',
  tenant_inactive: 'errors.tenantInactive',
};

/** Noma'lum kod ham blok sifatida ko'rsatiladi — mijoz sababsiz qolmasin. */
export function blockedReasonKey(code: string | null | undefined): string {
  return (code && BLOCKED_KEYS[code]) || 'errors.subscriptionSuspended';
}
