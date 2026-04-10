export interface Transaction {
  id: number;
  type: TransactionType;
  counterpartyId: number | null;
  counterpartyName: string | null;
  transferId: number | null;
  amount: number;
  description: string | null;
  date: string;
  recordedByUserId: number;
  recordedByUserName: string;
  createdAt: string;
}

export enum TransactionType {
  Income = 1,
  Expense = 2
}

export interface TransactionCreateDto {
  type: TransactionType;
  counterpartyId: number | null;
  transferId: number | null;
  amount: number;
  description: string | null;
  date: string;
}

export interface Debt {
  id: number;
  counterpartyId: number;
  counterpartyName: string;
  counterpartyType: string;
  amount: number;
  updatedAt: string;
}

export interface PaymentCreateDto {
  counterpartyId: number;
  transferId: number | null;
  amount: number;
  method: number;
  note: string | null;
}

export interface FinanceSummary {
  totalIncome: number;
  totalExpense: number;
  netAmount: number;
  totalDebt: number;
}
