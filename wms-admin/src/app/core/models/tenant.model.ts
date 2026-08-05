export enum SubscriptionStatus {
  Trial = 1,
  Active = 2,
  Suspended = 3
}

/** Nega to'xtatilgan — mijozga ko'rsatiladigan matnni ham shu belgilaydi. */
export type SuspendReason = 'NonPayment' | 'ClientRequest' | 'Technical' | 'Violation' | 'Other';

export const SUSPEND_REASONS: { label: string; value: SuspendReason }[] = [
  { label: 'Non-payment', value: 'NonPayment' },
  { label: 'Client request', value: 'ClientRequest' },
  { label: 'Technical', value: 'Technical' },
  { label: 'Violation', value: 'Violation' },
  { label: 'Other', value: 'Other' }
];

export interface Tenant {
  id: number;
  name: string;
  slug: string;
  isActive: boolean;
  planType: string | null;
  planId: number | null;
  planName: string | null;
  subscriptionStatus: SubscriptionStatus;
  trialEndsAt: string | null;
  /** To'langan muddat oxiri. null → muddat cheklovi yo'q. */
  paidUntil: string | null;
  suspendReason: SuspendReason | null;
  suspendedUntil: string | null;
  suspendPublicMessage: string | null;
  suspendNote: string | null;
  inn: string | null;
  createdAt: string;
  userCount: number;
}

export interface CreateTenantDto {
  name: string;
  slug: string;
  adminFullName: string;
  adminPhone: string;
  adminPassword: string;
  planId: number | null;
  inn?: string | null;
  paidUntil?: string | null;
}

export interface UpdateTenantDto {
  name: string;
  slug: string;
  isActive: boolean;
  planId: number | null;
  subscriptionStatus: SubscriptionStatus;
  trialEndsAt: string | null;
  inn?: string | null;
}

export interface SuspendTenantDto {
  reason: SuspendReason;
  /** Ichki izoh — mijozga hech qachon ko'rsatilmaydi. */
  note: string | null;
  /** Mijoz ko'radigan matn. */
  publicMessage: string | null;
  /** Shu sanada avtomatik qayta yoqiladi. null → muddatsiz. */
  until: string | null;
}

export interface TenantModuleInfo {
  moduleId: number;
  moduleName: string;
  moduleCode: string;
  isEnabled: boolean;
}

export type PaymentMethod = 'Cash' | 'BankTransfer' | 'Card' | 'Other';

export const PAYMENT_METHODS: { label: string; value: PaymentMethod }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank transfer', value: 'BankTransfer' },
  { label: 'Card', value: 'Card' },
  { label: 'Other', value: 'Other' }
];

export interface PaymentRecord {
  id: number;
  tenantId: number;
  periodStart: string;
  periodEnd: string;
  amount: number;
  currency: string;
  method: PaymentMethod;
  note: string | null;
  recordedByName?: string | null;
  recordedAt: string;
}

export interface CreatePaymentDto {
  periodStart: string;
  periodEnd: string;
  amount: number;
  currency: string;
  method: PaymentMethod;
  note: string | null;
}

/** `GET admin/tenants/expiring` — muddati yaqinlashgan yoki o'tgan tenantlar. */
export interface ExpiringTenant {
  tenantId: number;
  name: string;
  slug: string;
  planName: string | null;
  status: SubscriptionStatus;
  paidUntil: string | null;
  trialEndsAt: string | null;
  /** Manfiy — muddat o'tgan (grace davrida). */
  daysLeft: number;
  kind: 'paid' | 'trial';
}
