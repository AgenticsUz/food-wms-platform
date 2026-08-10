import { Injectable, inject, signal, computed } from '@angular/core';
import { map, tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  SubscriptionInfo, SubscriptionLimits, SubscriptionPlan, LimitDetail, WARN_BEFORE_DAYS
} from '../models/subscription.model';
import { ApiResponse } from '../models/api-response.model';

/**
 * Tenant o'z obunasi. `subscription/me` enforcement middleware'dan ozod —
 * bloklangan mijoz ham sababni ko'ra oladi, ya'ni bu so'rov 402 bermaydi.
 */
@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private api = inject(ApiService);

  info = signal<SubscriptionInfo | null>(null);
  loading = signal(false);

  /** Login javobi ham feature ro'yxatini beradi; sahifa yangilanganda shu signal manba bo'ladi. */
  enabledFeatures = computed(() => this.info()?.enabledFeatures ?? []);

  /**
   * Muddat tugashiga oz qoldimi. To'lov muddati trialdan ustun —
   * mijoz to'lagan bo'lsa demo sanasi endi ahamiyatsiz.
   */
  daysLeft = computed(() => {
    const i = this.info();
    if (!i) return null;
    return i.paidUntil ? i.daysUntilPaidEnd : i.daysUntilTrialEnd;
  });

  isExpiringSoon = computed(() => {
    const d = this.daysLeft();
    return d !== null && d >= 0 && d <= WARN_BEFORE_DAYS;
  });

  /** To'lov/demo muddati o'tgan, lekin grace davri hali tugamagan. */
  inGrace = computed(() => {
    const d = this.daysLeft();
    return d !== null && d < 0 && !(this.info()?.isBlocked ?? false);
  });

  load() {
    this.loading.set(true);
    return this.fetch().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });
  }

  fetch() {
    return this.api.get<unknown>('subscription/me').pipe(
      map(res => normalizeResponse(res)),
      tap(res => { if (res.success && res.data) this.info.set(res.data); })
    );
  }

  plans() {
    return this.api.get<SubscriptionPlan[]>('subscription/plans');
  }

  clear() {
    this.info.set(null);
  }
}

const EMPTY_DETAIL: LimitDetail = { max: 0, current: 0, usagePercent: null, isNearLimit: null };

const EMPTY_LIMITS: SubscriptionLimits = {
  maxUsers: 0, currentUsers: 0,
  maxWarehouses: 0, currentWarehouses: 0,
  maxTransfersPerMonth: 0, currentTransfersThisMonth: 0,
  users: EMPTY_DETAIL, warehouses: EMPTY_DETAIL, transfers: EMPTY_DETAIL
};

const STATUS_NAMES: Record<number, string> = { 1: 'Trial', 2: 'Active', 3: 'Suspended' };

/**
 * Backend bu shaklga bosqichma-bosqich o'tmoqda. Eski javob (raqamli `status`,
 * massiv ko'rinishidagi `limits`) ham qabul qilinadi, aks holda backend
 * yangilanmaguncha sahifa bo'sh qolardi.
 */
function normalizeResponse(res: ApiResponse<unknown>): ApiResponse<SubscriptionInfo> {
  if (!res.success || !res.data) return res as ApiResponse<SubscriptionInfo>;
  const raw = res.data as Record<string, any>;

  const status = typeof raw['status'] === 'number'
    ? (STATUS_NAMES[raw['status']] ?? 'Active')
    : (raw['status'] ?? 'Active');

  return { ...res, data: {
    tenantName: raw['tenantName'] ?? '',
    planName: raw['planName'] ?? null,
    planCode: raw['planCode'] ?? null,
    status,

    trialEndsAt: raw['trialEndsAt'] ?? null,
    daysUntilTrialEnd: raw['daysUntilTrialEnd'] ?? raw['trialDaysLeft'] ?? null,

    paidUntil: raw['paidUntil'] ?? null,
    daysUntilPaidEnd: raw['daysUntilPaidEnd'] ?? null,
    paymentGraceDays: raw['paymentGraceDays'] ?? raw['graceDays'] ?? 0,

    isBlocked: raw['isBlocked'] ?? false,
    blockedReason: raw['blockedReason'] ?? null,
    blockedMessage: raw['blockedMessage'] ?? null,
    suspendedUntil: raw['suspendedUntil'] ?? null,

    limits: normalizeLimits(raw['limits']),
    enabledModules: raw['enabledModules'] ?? [],
    enabledFeatures: raw['enabledFeatures'] ?? [],

    supportPhone: raw['supportPhone'] ?? null,
    supportEmail: raw['supportEmail'] ?? null
  } };
}

function normalizeLimits(limits: unknown): SubscriptionLimits {
  if (!limits) return EMPTY_LIMITS;

  if (!Array.isArray(limits)) {
    const raw = limits as Record<string, any>;
    const merged = { ...EMPTY_LIMITS, ...(raw as SubscriptionLimits) };
    // Detal obyektlari eski backendda bo'lmasligi mumkin — o'shanda tekis maydonlardan
    // quramiz, lekin foizni **hisoblamaymiz**: u backendning ishi (`null` qoladi).
    merged.users = detail(raw['users'], merged.maxUsers, merged.currentUsers);
    merged.warehouses = detail(raw['warehouses'], merged.maxWarehouses, merged.currentWarehouses);
    merged.transfers = detail(raw['transfers'], merged.maxTransfersPerMonth, merged.currentTransfersThisMonth);
    return merged;
  }

  // Eski shakl: [{ key, limit, used }, ...]
  const find = (key: string) => (limits as { key: string; limit: number; used: number }[])
    .find(l => l.key === key);
  const users = find('users');
  const warehouses = find('warehouses');
  const transfers = find('transfersThisMonth');
  return {
    maxUsers: users?.limit ?? 0, currentUsers: users?.used ?? 0,
    maxWarehouses: warehouses?.limit ?? 0, currentWarehouses: warehouses?.used ?? 0,
    maxTransfersPerMonth: transfers?.limit ?? 0, currentTransfersThisMonth: transfers?.used ?? 0,
    users: detail(null, users?.limit ?? 0, users?.used ?? 0),
    warehouses: detail(null, warehouses?.limit ?? 0, warehouses?.used ?? 0),
    transfers: detail(null, transfers?.limit ?? 0, transfers?.used ?? 0)
  };
}

function detail(raw: unknown, max: number, current: number): LimitDetail {
  const d = raw as Partial<LimitDetail> | null | undefined;
  return {
    max: d?.max ?? max,
    current: d?.current ?? current,
    usagePercent: d?.usagePercent ?? null,
    isNearLimit: d?.isNearLimit ?? null
  };
}
