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

/** Muddat tugashiga necha kun qolganda ogohlantiramiz (backend `Subscription:WarnBeforeDays`). */
export const WARN_BEFORE_DAYS = 7;

/** Limitning necha foizidan boshlab sariq ko'rsatamiz. */
export const LIMIT_WARN_PERCENT = 80;

export interface SubscriptionLimits {
  maxUsers: number;
  currentUsers: number;
  maxWarehouses: number;
  currentWarehouses: number;
  maxTransfersPerMonth: number;
  currentTransfersThisMonth: number;
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

export function toLimitRows(limits: SubscriptionLimits | null): LimitRow[] {
  if (!limits) return [];
  const rows: { key: LimitRow['key']; used: number; limit: number }[] = [
    { key: 'users', used: limits.currentUsers, limit: limits.maxUsers },
    { key: 'warehouses', used: limits.currentWarehouses, limit: limits.maxWarehouses },
    { key: 'transfers', used: limits.currentTransfersThisMonth, limit: limits.maxTransfersPerMonth }
  ];
  return rows.map(r => {
    const isUnlimited = !r.limit || r.limit <= 0;
    const percent = isUnlimited ? 0 : Math.min(100, Math.round((r.used / r.limit) * 100));
    return {
      ...r,
      isUnlimited,
      percent,
      isExceeded: !isUnlimited && r.used >= r.limit,
      isNearLimit: !isUnlimited && percent >= LIMIT_WARN_PERCENT
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
