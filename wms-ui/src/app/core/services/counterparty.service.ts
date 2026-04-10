import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Counterparty,
  CounterpartyCreateDto,
  CounterpartyBalance,
  PaymentHistory
} from '../models/counterparty.model';
import { Transfer } from '../models/transfer.model';

@Injectable({ providedIn: 'root' })
export class CounterpartyService {
  private api = inject(ApiService);

  getCounterparties(params?: Record<string, string | number | boolean>) {
    return this.api.get<Counterparty[]>('counterparties', params);
  }

  getCounterparty(id: number) {
    return this.api.get<Counterparty>(`counterparties/${id}`);
  }

  createCounterparty(dto: CounterpartyCreateDto) {
    return this.api.post<Counterparty>('counterparties', dto);
  }

  updateCounterparty(id: number, dto: CounterpartyCreateDto) {
    return this.api.put<Counterparty>(`counterparties/${id}`, dto);
  }

  deleteCounterparty(id: number) {
    return this.api.delete<void>(`counterparties/${id}`);
  }

  getBalance(id: number) {
    return this.api.get<CounterpartyBalance>(`counterparties/${id}/balance`);
  }

  getPayments(id: number) {
    return this.api.get<PaymentHistory[]>(`counterparties/${id}/payments`);
  }

  getTransfers(id: number) {
    return this.api.get<Transfer[]>('transfers', { counterpartyId: id });
  }
}
