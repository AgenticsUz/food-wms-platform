import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private api = inject(ApiService);

  rates = signal<Record<string, number>>({});
  lastUpdated = signal<string>('');
  loading = signal(false);

  loadRates() {
    this.loading.set(true);
    this.api.get<{ rates: Record<string, number>; lastUpdated: string }>('currency/rates').subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.rates.set(res.data.rates);
          this.lastUpdated.set(res.data.lastUpdated);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  convert(amount: number, from: string, to: string): number {
    const r = this.rates();
    if (from === 'UZS' && to !== 'UZS') return amount / (r[to] ?? 1);
    if (from !== 'UZS' && to === 'UZS') return amount * (r[from] ?? 1);
    if (from !== 'UZS' && to !== 'UZS') return (amount * (r[from] ?? 1)) / (r[to] ?? 1);
    return amount;
  }
}
