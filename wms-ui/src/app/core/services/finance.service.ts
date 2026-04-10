import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Transaction, TransactionCreateDto,
  Debt, PaymentCreateDto, FinanceSummary
} from '../models/finance.model';
import { PaymentHistory } from '../models/counterparty.model';
import { IncomeExpenseDto } from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private api = inject(ApiService);

  getTransactions(params?: Record<string, string | number | boolean>) { return this.api.get<Transaction[]>('finance/transactions', params); }
  createTransaction(dto: TransactionCreateDto) { return this.api.post<Transaction>('finance/transactions', dto); }
  deleteTransaction(id: number) { return this.api.delete<void>(`finance/transactions/${id}`); }
  getDebts() { return this.api.get<Debt[]>('finance/debts'); }
  recordPayment(dto: PaymentCreateDto) { return this.api.post<PaymentHistory>('finance/payments', dto); }
  getPayments(params?: Record<string, string | number | boolean>) { return this.api.get<PaymentHistory[]>('finance/payments', params); }
  getSummary() { return this.api.get<FinanceSummary>('finance/summary'); }
  getIncomeExpense(days = 30) { return this.api.get<IncomeExpenseDto[]>('analytics/finance/income-expense', { days }); }
}
