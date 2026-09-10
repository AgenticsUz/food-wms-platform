import type { CounterpartyType } from '../counterparties/counterparty.model';

/**
 * Moliya modellari — manba `src/WMS.Application/DTOs/Finance/FinanceDtos.cs`
 * (+ `AnalyticsDtos.cs` dagi grafik DTO'lari).
 *
 * F6 dagi farqlar: id'lar Guid satr; tranzaksiyada `recordedByUserId`/`createdAt`
 * yo'q; qarzda `id`/`updatedAt` yo'q, `counterpartyType` — SON enum (eskisida satr);
 * xulosada `netAmount` → `netProfit`.
 */

export enum TransactionType {
  Income = 1,
  Expense = 2,
}

export interface Transaction {
  readonly id: string;
  readonly type: TransactionType;
  readonly counterpartyId: string | null;
  readonly counterpartyName: string | null;
  readonly transferId: string | null;
  readonly amount: number;
  readonly description: string | null;
  readonly date: string;
  readonly recordedByUserName: string;
}

export interface TransactionCreateDto {
  readonly type: TransactionType;
  readonly counterpartyId: string | null;
  readonly transferId: string | null;
  readonly amount: number;
  readonly description: string | null;
  /** Faqat sana (`YYYY-MM-DD`, mahalliy kun). */
  readonly date: string;
}

export interface Debt {
  readonly counterpartyId: string;
  readonly counterpartyName: string;
  readonly counterpartyType: CounterpartyType;
  /** Musbat — ular bizga qarzdor, manfiy — biz ularga. */
  readonly amount: number;
}

export enum PaymentMethod {
  Cash = 1,
  Bank = 2,
  Card = 3,
}

/**
 * `direction` (`PaymentDirection`) ataylab YO'Q: eski ekran uni yubormagan va backend
 * yo'nalishni qarz belgisidan o'zi aniqlaydi (`CreatePaymentDto` izohi).
 */
export interface PaymentCreateDto {
  readonly counterpartyId: string;
  readonly transferId: string | null;
  readonly amount: number;
  readonly method: PaymentMethod;
  readonly note: string | null;
}

export interface PaymentHistory {
  readonly id: string;
  readonly counterpartyId: string;
  readonly counterpartyName: string;
  readonly transferId: string | null;
  readonly amount: number;
  readonly method: PaymentMethod;
  readonly paidAt: string;
  readonly note: string | null;
  readonly recordedByUserName: string;
}

export interface FinanceSummary {
  readonly totalIncome: number;
  readonly totalExpense: number;
  readonly totalDebt: number;
  readonly netProfit: number;
}

/** `GET analytics/finance/income-expense`. */
export interface IncomeExpensePoint {
  readonly date: string;
  readonly income: number;
  readonly expense: number;
  readonly net: number;
}

/** `GET analytics/finance/top-debtors` (`type` — son enum). */
export interface TopDebtor {
  readonly counterpartyName: string;
  readonly type: CounterpartyType;
  readonly debtAmount: number;
}

/** To'lov usuli nomi kaliti — to'lovlar va kontragent sahifalari uchun bitta manba. */
export function paymentMethodKey(method: PaymentMethod): string {
  switch (method) {
    case PaymentMethod.Bank:
      return 'finance.bank';
    case PaymentMethod.Card:
      return 'finance.card';
    default:
      return 'finance.cash';
  }
}
