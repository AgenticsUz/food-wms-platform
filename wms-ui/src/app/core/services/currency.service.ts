import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface CurrencyRatesResponse {
  rates: Record<string, number>;
  lastUpdated: string;
  source: string;
}

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private http = inject(HttpClient);

  rates = signal<Record<string, number>>({});
  lastUpdated = signal<string>('');
  loading = signal(false);

  loadRates() {
    this.loading.set(true);
    this.http.get<CurrencyRatesResponse>(`${environment.apiUrl}/currency/rates`).subscribe({
      next: (res) => {
        if (res && res.rates) {
          this.rates.set(res.rates);
          this.lastUpdated.set(res.lastUpdated ?? '');
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
