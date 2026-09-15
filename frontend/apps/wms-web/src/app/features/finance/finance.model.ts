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

/** Pul yo'nalishi (`WMS.Domain/Enums/PaymentDirection.cs`). */
export enum PaymentDirection {
  /** Pul BIZGA tushdi — ularning qarzi kamayadi. */
  In = 1,
  /** Biz TO'LADIK — bizning qarzimiz kamayadi. */
  Out = 2,
}

/** Yozuv qaysi yuzadan kirgan (`WMS.Domain/Enums/DocumentSource.cs`). */
export enum DocumentSource {
  Ui = 1,
  Telegram = 2,
  Ai = 3,
}

/**
 * ⚠️ `direction` bu yerda MAJBURIY (backendda `PaymentDirection?`), chunki bo'sh
 * yuborilganda server yo'nalishni qarz belgisidan taxmin qiladi — va balans NOL
 * bo'lsa taxmin qilolmay 400 beradi. Taxmin xato bo'lsa summa teskari tomonga
 * yozilib xatoni ikki barobar qilardi, shuning uchun tanlovni ekran so'raydi
 * (P2.8; `CreatePaymentDto.Direction` izohi bilan bir xil sabab).
 *
 * `documentDate` — kalendar kuni (`YYYY-MM-DD`), vaqt nuqtasi emas: hisobot shu
 * sana bo'yicha yig'iladi. Bo'sh — bugun; kelajak sana 400; o'tgan sana
 * `documents.backdate` ruxsati bilan.
 */
export interface PaymentCreateDto {
  readonly counterpartyId: string;
  readonly transferId: string | null;
  readonly amount: number;
  readonly method: PaymentMethod;
  readonly direction: PaymentDirection;
  readonly documentDate: string | null;
  readonly note: string | null;
}

export interface PaymentHistory {
  readonly id: string;
  readonly counterpartyId: string;
  readonly counterpartyName: string;
  readonly transferId: string | null;
  readonly amount: number;
  readonly method: PaymentMethod;
  readonly direction: PaymentDirection;
  /** Hujjat sanasi — ro'yxatdagi «sana» ustuni shu (hisobot shu bo'yicha). */
  readonly documentDate: string;
  /** Yozuv yaratilgan lahza — audit izi, sana ustuni EMAS. */
  readonly paidAt: string;
  readonly source: DocumentSource;
  /** To'ldirilgan bo'lsa — bu qator storno (boshqa to'lovni qaytargan). */
  readonly reversalOfId: string | null;
  readonly reversalReason: string | null;
  /** Shu to'lov keyinchalik qaytarilganmi. */
  readonly isReversed: boolean;
  readonly note: string | null;
  readonly recordedByUserName: string;
}

/** Storno so'rovi tanasi — sabab majburiy va tarixda ko'rinadi. */
export interface PaymentReverseDto {
  readonly reason: string;
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

/** Yo'nalish nomi kaliti — forma tugmasi ham, ro'yxat belgisi ham shundan. */
export function paymentDirectionKey(direction: PaymentDirection): string {
  return direction === PaymentDirection.In ? 'finance.paymentIn' : 'finance.paymentOut';
}

/** Yo'nalish rangi: kirim — yashil, chiqim — qizil (`status-badge` kalitlari). */
export function paymentDirectionStatus(direction: PaymentDirection): string {
  return direction === PaymentDirection.In ? 'Income' : 'Expense';
}

/**
 * Manba belgisi. `Ui` — belgi YO'Q (odatiy holat shovqin qilmasin), Telegram va
 * AI esa ko'rinsin: AI kiritgan yozuvni ajratib ko'rish F10 talabi.
 */
export function documentSourceIcon(source: DocumentSource): string | null {
  switch (source) {
    case DocumentSource.Telegram:
      return 'pi pi-telegram';
    case DocumentSource.Ai:
      return 'pi pi-sparkles';
    default:
      return null;
  }
}

/** Manba nomi kaliti (belgi ustidagi izoh uchun). */
export function documentSourceKey(source: DocumentSource): string {
  switch (source) {
    case DocumentSource.Telegram:
      return 'finance.sourceTelegram';
    case DocumentSource.Ai:
      return 'finance.sourceAi';
    default:
      return 'finance.sourceUi';
  }
}
