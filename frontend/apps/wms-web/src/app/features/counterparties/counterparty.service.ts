import { Injectable, inject } from '@angular/core';

import { ApiService, type ApiCallOptions } from '../../core/api/api.service';
import type { TelegramLinkApi } from '../../shared/components/telegram-link-dialog/telegram-link-dialog.component';
import type { TelegramLinkToken, TelegramSubjectLink } from '../settings/settings.model';
import type { PaymentHistory } from '../finance/finance.model';
import type {
  Counterparty,
  CounterpartyBalance,
  CounterpartySaveDto,
  CounterpartyTransfer,
  CounterpartyType,
} from './counterparty.model';

/** Kontragentlar API'si (eski `core/services/counterparty.service.ts`). */
@Injectable({ providedIn: 'root' })
export class CounterpartyService {
  private readonly api = inject(ApiService);

  /** Kontragent Telegram'i (TG13) — kartadan havola; xabarlar tenant sozlamasi yoqiq bo'lsa keladi. */
  telegram(id: string): TelegramLinkApi {
    return {
      get: () => this.api.get<TelegramSubjectLink>(`counterparties/${id}/telegram`),
      create: () => this.api.post<TelegramLinkToken>(`counterparties/${id}/telegram-link`, {}),
      unlink: () => this.api.delete<void>(`counterparties/${id}/telegram`),
    };
  }

  /**
   * `search` — SERVER qidiruvi (pg_trgm + lotin↔kirill transliteratsiya). Nega
   * mijozda emas: `name.includes(q)` alifboni bilmaydi — «Алишер» yozgan odam
   * «Alisher» ni topa olmasdi. Bo'sh qidiruvda parametr umuman yuborilmaydi va
   * server eski xatti-harakatini (to'liq ro'yxat) beradi.
   */
  getCounterparties(type?: CounterpartyType, search?: string, options?: ApiCallOptions) {
    return this.api.get<Counterparty[]>(
      'counterparties',
      { type, search: search?.trim() || undefined },
      options
    );
  }
  getCounterparty(id: string) {
    return this.api.get<Counterparty>(`counterparties/${id}`);
  }
  createCounterparty(dto: CounterpartySaveDto) {
    return this.api.post<Counterparty>('counterparties', dto);
  }
  updateCounterparty(id: string, dto: CounterpartySaveDto) {
    return this.api.put<Counterparty>(`counterparties/${id}`, dto);
  }
  deleteCounterparty(id: string) {
    return this.api.delete<null>(`counterparties/${id}`);
  }
  getBalance(id: string) {
    return this.api.get<CounterpartyBalance>(`counterparties/${id}/balance`);
  }
  getPayments(id: string) {
    return this.api.get<PaymentHistory[]>(`counterparties/${id}/payments`);
  }

  /**
   * O'tkazmalar tarixi — `TRANSFERS` moduli va `transfers.view` ruxsati ortida.
   * Kontragentlar esa modulsiz ochiladi; o'tkazmasiz tarifda sahifa 403 toasti
   * bilan ochilmasin — jadval shunchaki bo'sh qoladi (`skipErrorNotify`).
   * `pageSize` — backend standarti 50, eski jadval hammasini ko'rsatardi.
   */
  getTransfers(id: string) {
    return this.api.get<CounterpartyTransfer[]>(
      'transfers',
      { counterpartyId: id, page: 1, pageSize: 1000 },
      { skipErrorNotify: true }
    );
  }
}
