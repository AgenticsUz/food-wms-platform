import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { httpFlags } from '@agentics/http';

import { isApiResponse } from '../api/api-response.model';

interface CurrencyRates {
  readonly rates: Readonly<Record<string, number>>;
  readonly lastUpdated?: string | null;
  readonly source?: string | null;
}

/**
 * Valyuta kurslari (topbar vidjeti va moliya ekranlari) — eski `currency.service.ts`.
 *
 * `GET currency/rates` eski backendda konvertsiz (`{ rates, lastUpdated }`)
 * qaytardi; ko'chgan backend konvert bilan qaytarishi mumkin — ikkalasi ham
 * o'qiladi. So'rov JIM (`skipErrorNotify` + `skipLoading`): kurs — fon
 * ma'lumoti, uning yo'qligi har sahifada qizil toast bo'lib chiqmasligi kerak.
 */
@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly http = inject(HttpClient);

  readonly rates = signal<Readonly<Record<string, number>>>({});
  readonly lastUpdated = signal<string>('');
  readonly loading = signal(false);

  loadRates(): void {
    this.loading.set(true);
    this.http
      .get<unknown>('currency/rates', {
        context: httpFlags({ skipErrorNotify: true, skipLoading: true }),
      })
      .subscribe({
        next: (body) => {
          const payload = (isApiResponse(body) ? body.data : body) as CurrencyRates | null;
          if (payload?.rates) {
            this.rates.set(payload.rates);
            this.lastUpdated.set(payload.lastUpdated ?? '');
          }
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
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
