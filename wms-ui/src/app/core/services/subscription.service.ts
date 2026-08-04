import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { SubscriptionInfo, SubscriptionPlan } from '../models/subscription.model';

/**
 * Tenant o'z obunasi. `subscription/me` enforcement middleware'dan ozod —
 * bloklangan mijoz ham sababni ko'ra oladi, shuning uchun bu so'rov 402 bermaydi.
 */
@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private api = inject(ApiService);

  info = signal<SubscriptionInfo | null>(null);
  loading = signal(false);

  load() {
    this.loading.set(true);
    return this.api.get<SubscriptionInfo>('subscription/me').subscribe({
      next: (res) => {
        if (res.success && res.data) this.info.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  /** Sahifa uchun — javobni kuzatish kerak bo'lganda. */
  fetch() {
    return this.api.get<SubscriptionInfo>('subscription/me').pipe(
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
