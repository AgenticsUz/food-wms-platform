import { Injectable, inject, signal, effect } from '@angular/core';
import { SubscriptionService } from './subscription.service';

const STORAGE_KEY = 'enabledFeatures';

/**
 * Feature — moduldan mayda dona: bitta sahifa darajasida yoqiladi yoki yopiladi.
 * Uch qatlam: Plan (nima sotildi) → Feature (tenantda bormi) → Permission (kim ishlatadi).
 *
 * Ro'yxat bo'sh bo'lsa hech narsa yopilmaydi. Bu ataylab: backend feature
 * qatlamini yubormaguncha (yoki eski tenantda katalog bo'lmasa) menyu
 * bugungidek qolishi kerak — cheklov qo'shish regressiya keltirmasin.
 */
@Injectable({ providedIn: 'root' })
export class FeatureService {
  private subscription = inject(SubscriptionService);

  enabledFeatures = signal<string[]>(this.restore());

  constructor() {
    // `subscription/me` javobi yangilanganda ro'yxat ham yangilanadi
    effect(() => {
      const list = this.subscription.info()?.enabledFeatures;
      if (list) this.setFeatures(list);
    });
  }

  isEnabled(code: string): boolean {
    const list = this.enabledFeatures();
    return list.length === 0 || list.includes(code);
  }

  setFeatures(codes: string[]) {
    this.enabledFeatures.set(codes);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(codes));
  }

  clear() {
    this.enabledFeatures.set([]);
    localStorage.removeItem(STORAGE_KEY);
  }

  private restore(): string[] {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return [];
    try {
      const parsed = JSON.parse(stored);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
}
