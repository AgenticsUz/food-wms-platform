import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { tap } from 'rxjs';

import { ApiService, type ApiCallOptions } from '../../../core/api/api.service';
import type { SubscriptionInfo, SubscriptionPlan } from './subscription.model';

/**
 * Tenantning TO'LIQ obunasi (`subscription/me`): limitlar, grace, planlar.
 *
 * Qobiqqa kerakli qisqa holat `WmsSession.subscription()` da (`/api/me`) — bu
 * servis faqat Obuna ekrani va `app-limit-notice` uchun. `subscription/me`
 * enforcement'dan ozod: bloklangan mijoz ham sababni ko'ra oladi.
 *
 * Eski ilova brendni ham shu yerdan tiklardi — endi brend `/api/me` dan (D14).
 */
@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly api = inject(ApiService);

  private readonly state = signal<SubscriptionInfo | null>(null);
  readonly info = this.state.asReadonly();

  /** To'lov muddati trialdan ustun — mijoz to'lagan bo'lsa demo sanasi ahamiyatsiz. */
  readonly daysLeft = computed(() => {
    const info = this.state();
    if (info === null) return null;
    return info.paidUntil ? info.daysUntilPaidEnd : info.daysUntilTrialEnd;
  });

  /** Muddat o'tgan, lekin grace davri hali tugamagan. */
  readonly inGrace = computed(() => {
    const days = this.daysLeft();
    return days !== null && days < 0 && !(this.state()?.isBlocked ?? false);
  });

  constructor() {
    // Limitga yaqinlashish ogohlantirishi kelsa (ombor/transfer yaratilgach) hisob
    // eskirgan: qayta o'qiymiz. Eski `refreshLimits` o'rniga — yaratish ekranlari
    // bu servisni chaqirishi shart emas (aylanma bog'liqlik bo'lmaydi).
    effect(() => {
      const warning = this.api.lastWarning();
      if (warning?.code.startsWith('limit_warn') && untracked(this.state) !== null) {
        untracked(() => this.fetch({ skipLoading: true }).subscribe());
      }
    });
  }

  fetch(options?: ApiCallOptions) {
    return this.api.get<SubscriptionInfo>('subscription/me', undefined, options).pipe(
      tap((res) => {
        if (res.success && res.data) this.state.set(res.data);
      })
    );
  }

  /** Hali o'qilmagan bo'lsa bir marta o'qiydi (fon so'rovi — toast va progress yo'q). */
  ensureLoaded(): void {
    if (this.state() === null) {
      this.fetch({ skipLoading: true, skipErrorNotify: true }).subscribe({ error: () => undefined });
    }
  }

  plans() {
    return this.api.get<SubscriptionPlan[]>('subscription/plans');
  }
}
