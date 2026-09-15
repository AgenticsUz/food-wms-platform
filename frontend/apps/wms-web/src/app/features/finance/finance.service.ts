import { Injectable, inject } from '@angular/core';

import { ApiService, type QueryParams } from '../../core/api/api.service';
import type { Counterparty } from '../counterparties/counterparty.model';
import type {
  Debt,
  FinanceSummary,
  IncomeExpensePoint,
  PaymentCreateDto,
  PaymentHistory,
  PaymentReverseDto,
  TopDebtor,
  Transaction,
  TransactionCreateDto,
} from './finance.model';

/**
 * Backend ro'yxatlari F6 da sahifalangan (`pageSize` standarti 20/50), eski ekranlar
 * esa BUTUN ro'yxatni olib, sahifalashni `p-table` da qiladi. Standart qolsa 21-
 * tranzaksiya jimgina ko'rinmay qolardi — shuning uchun katta sahifa so'raladi.
 */
const ALL_ROWS = { page: 1, pageSize: 1000 } as const;

/** Moliya API'si (eski `core/services/finance.service.ts`). */
@Injectable({ providedIn: 'root' })
export class FinanceService {
  private readonly api = inject(ApiService);

  getTransactions(params?: QueryParams) {
    return this.api.get<Transaction[]>('finance/transactions', { ...ALL_ROWS, ...params });
  }
  createTransaction(dto: TransactionCreateDto) {
    return this.api.post<Transaction>('finance/transactions', dto);
  }
  deleteTransaction(id: string) {
    return this.api.delete<null>(`finance/transactions/${id}`);
  }
  getDebts() {
    return this.api.get<Debt[]>('finance/debts');
  }
  recordPayment(dto: PaymentCreateDto) {
    return this.api.post<PaymentHistory>('finance/payments', dto);
  }
  getPayments() {
    return this.api.get<PaymentHistory[]>('finance/payments', ALL_ROWS);
  }
  /**
   * Storno — to'lov O'CHIRILMAYDI, teskari yozuv qo'shiladi (P2.9). Sabab
   * majburiy: tarixda «nega qaytarildi» degan savolga javob shu qatorda qoladi.
   * Xato matnini server tayyor (tarjima qilingan) holda qaytaradi.
   */
  reversePayment(id: string, reason: string) {
    const body: PaymentReverseDto = { reason };
    return this.api.post<PaymentHistory>(`finance/payments/${id}/reverse`, body);
  }
  getSummary() {
    return this.api.get<FinanceSummary>('finance/summary');
  }

  /**
   * Grafiklar `analytics.advanced` feature'i ortida (+ `dashboard.view` ruxsati).
   * Ular xulosa sahifasining bezagi: rad etilsa sahifa qizil toastsiz, grafiksiz
   * ochilishi kerak — shuning uchun `skipErrorNotify`.
   */
  getIncomeExpense(days = 30) {
    return this.api.get<IncomeExpensePoint[]>(
      'analytics/finance/income-expense',
      { days },
      { skipErrorNotify: true }
    );
  }
  getTopDebtors(top = 5) {
    return this.api.get<TopDebtor[]>('analytics/finance/top-debtors', { top }, { skipErrorNotify: true });
  }

  /** Forma tanlovi uchun (kontragentlar bo'limi endpoint'i). */
  getCounterparties() {
    return this.api.get<Counterparty[]>('counterparties');
  }
}
