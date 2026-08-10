import { Branding } from '../services/branding.service';

/** Bloklanish sabablari — backend `code` maydonidagi qiymatlar bilan bir xil. */
export type BlockedReason =
  | 'trial_expired'
  | 'payment_expired'
  | 'suspended_nonpayment'
  | 'suspended_request'
  | 'suspended_technical'
  | 'suspended_violation'
  | 'suspended_other'
  | 'tenant_inactive';

/** Blok sababi → tarjima kaliti. Interceptor, login va Obuna sahifasi shu bitta xaritaga tayanadi. */
const BLOCKED_KEYS: Record<string, string> = {
  trial_expired: 'errors.trialExpired',
  payment_expired: 'errors.paymentExpired',
  suspended_nonpayment: 'errors.suspendedNonpayment',
  suspended_request: 'errors.suspendedRequest',
  suspended_technical: 'errors.suspendedTechnical',
  suspended_violation: 'errors.suspendedViolation',
  suspended_other: 'errors.suspendedOther',
  tenant_inactive: 'errors.tenantInactive'
};

/** Noma'lum kod ham blok sifatida ko'rsatiladi — mijoz sababsiz qolmasin. */
export function blockedReasonKey(code: string | null | undefined): string {
  return (code && BLOCKED_KEYS[code]) || 'errors.subscriptionSuspended';
}

export function isBlockingCode(code: string | null | undefined): boolean {
  return !!code && code in BLOCKED_KEYS;
}

/** Plan limiti kodlari — bular blok emas, faqat xabar. */
export const LIMIT_KEYS: Record<string, string> = {
  limit_users: 'errors.limitUsers',
  limit_warehouses: 'errors.limitWarehouses',
  limit_transfers: 'errors.limitTransfers'
};

/** Muddat tugashiga necha kun qolganda ogohlantiramiz (backend `Subscription:WarnBeforeDays`). */
export const WARN_BEFORE_DAYS = 7;

/**
 * Bitta limit — foiz va ogohlantirish chegarasi **backendda** hisoblanadi
 * (`Subscription:LimitWarnPercent`). Bu yerda qayta hisoblamang: ikki joydagi hisob
 * vaqt o'tib ajralib ketadi va konfig o'zgarganda UI ergashmay qoladi.
 *
 * Plansiz tenantda `usagePercent` va `isNearLimit` — `null`: cheklov ham,
 * ogohlantirish ham yo'q.
 */
export interface LimitDetail {
  max: number;
  current: number;
  usagePercent: number | null;
  isNearLimit: boolean | null;
}

export interface SubscriptionLimits {
  maxUsers: number;
  currentUsers: number;
  maxWarehouses: number;
  currentWarehouses: number;
  maxTransfersPerMonth: number;
  currentTransfersThisMonth: number;

  users: LimitDetail;
  warehouses: LimitDetail;
  transfers: LimitDetail;
}

export interface SubscriptionInfo {
  tenantName: string;
  planName: string | null;      // null = plan biriktirilmagan (cheklovsiz)
  planCode: string | null;
  status: string;               // "Trial" | "Active" | "Suspended"

  trialEndsAt: string | null;   // ISO, UTC
  daysUntilTrialEnd: number | null;

  paidUntil: string | null;     // ISO, UTC
  daysUntilPaidEnd: number | null;
  paymentGraceDays: number;

  isBlocked: boolean;
  blockedReason: BlockedReason | null;
  /** Backend bergan matn — standart tarjimadan ustun turadi. */
  blockedMessage: string | null;
  suspendedUntil: string | null;

  limits: SubscriptionLimits;
  enabledModules: string[];
  enabledFeatures: string[];

  supportPhone: string | null;
  supportEmail: string | null;

  /** Login javobidagi bilan bir xil obyekt — sahifa yangilanganda brend shu yerdan tiklanadi. */
  branding?: Branding | null;
}

/** Sahifada ko'rsatiladigan bitta limit qatori. */
export interface LimitRow {
  key: 'users' | 'warehouses' | 'transfers';
  used: number;
  limit: number;
  isUnlimited: boolean;
  percent: number;
  isExceeded: boolean;
  isNearLimit: boolean;
}

/**
 * Foiz ham, "limitga yaqin" bahosi ham **backenddan** olinadi. Bu yerda faqat
 * ko'rsatish uchun shakl beriladi — hech qanday hisob yo'q.
 */
export function toLimitRows(limits: SubscriptionLimits | null): LimitRow[] {
  if (!limits) return [];
  const rows: { key: LimitRow['key']; detail: LimitDetail }[] = [
    { key: 'users', detail: limits.users },
    { key: 'warehouses', detail: limits.warehouses },
    { key: 'transfers', detail: limits.transfers }
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
      isNearLimit: detail.isNearLimit === true
    };
  });
}

/** `GET subscription/plans` — mavjud planlar (upgrade ekrani uchun). */
export interface SubscriptionPlan {
  id: number;
  name: string;
  code: string;
  price: number;
  isActive: boolean;
  moduleCodes: string[];
  maxUsers: number;
  maxWarehouses: number;
  maxTransfersPerMonth: number;
  tenantCount: number;
  trialDays: number;
  isDefault: boolean;
}
