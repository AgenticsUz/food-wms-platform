export enum SubscriptionStatusCode {
  Trial = 1,
  Active = 2,
  Suspended = 3
}

/** Bloklanish sabablari — backend `code` maydonidagi qiymatlar bilan bir xil. */
export type BlockedReason = 'subscription_suspended' | 'trial_expired' | 'tenant_inactive';

export interface LimitUsage {
  key: 'users' | 'warehouses' | 'transfersThisMonth';
  limit: number;          // 0 = cheksiz
  used: number;
  isUnlimited: boolean;
  percent: number;        // 0..100 (cheksizda 0)
  isExceeded: boolean;
}

export interface SubscriptionInfo {
  tenantId: number;
  tenantName: string;
  slug: string;
  planId: number | null;
  planName: string | null;      // null = plan biriktirilmagan (cheklovsiz)
  planCode: string | null;
  planPrice: number;
  status: SubscriptionStatusCode;
  isActive: boolean;
  trialEndsAt: string | null;   // ISO, UTC
  trialDaysLeft: number | null; // manfiy bo'lsa — grace davrida
  graceDays: number;
  isExpiringSoon: boolean;      // trial 7 kundan kam qolgan → banner
  isBlocked: boolean;
  blockedReason: BlockedReason | null;
  enabledModules: string[];
  limits: LimitUsage[];
  limitWarnPercent: number;
}

/** `GET subscription/plans` — upgrade ekrani uchun faol planlar (backend `PlanDto`). */
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
