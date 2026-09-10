/**
 * Agent modellari — manba `src/WMS.Application/DTOs/Agents/AgentDtos.cs`.
 *
 * F6 da O'CHDI: agent portali (login, profil) va `portalEnabled`/`portalPhone`/
 * `portalPassword` maydonlari (D8) — Identity'dan tashqari ikkinchi login reyestri.
 */

export interface Agent {
  readonly id: string;
  readonly name: string;
  readonly phone: string | null;
  readonly commissionPercent: number;
  readonly isActive: boolean;
  readonly salesCount: number;
  readonly totalSales: number;
  readonly totalCommission: number;
  readonly commissionPaid: number;
  readonly commissionDue: number;
}

/** `CreateAgentDto` va `UpdateAgentDto` — shakli bir xil. */
export interface AgentSaveDto {
  readonly name: string;
  readonly phone: string | null;
  readonly commissionPercent: number;
  readonly isActive: boolean;
}

export enum CommissionStatus {
  /** Sotuv tasdiqlangan, mijoz hali to'lamagan (nasiya). */
  Pending = 1,
  /** Mijoz to'lagan — komissiya haqiqiy. */
  Confirmed = 2,
  /** Tovar qaytarilgan — komissiya yo'q. */
  Cancelled = 3,
}

export interface CommissionRecord {
  readonly id: string;
  readonly agentId: string;
  readonly agentName: string;
  readonly transferId: string;
  readonly counterpartyName: string | null;
  readonly saleAmount: number;
  readonly commissionPercent: number;
  readonly commissionAmount: number;
  readonly status: CommissionStatus;
  readonly isPaid: boolean;
  readonly paidAt: string | null;
  readonly createdAt: string;
}

export interface AgentSalesPoint {
  readonly date: string;
  readonly saleAmount: number;
  readonly commissionAmount: number;
}

export interface AgentSalesReport {
  readonly agentId: string;
  readonly agentName: string;
  readonly commissionPercent: number;
  readonly salesCount: number;
  readonly totalSales: number;
  readonly totalCommission: number;
  readonly commissionConfirmed: number;
  readonly commissionPending: number;
  readonly commissionCancelled: number;
  readonly commissionPaid: number;
  readonly commissionDue: number;
  readonly timeline: readonly AgentSalesPoint[];
}

export interface PayCommissionDto {
  readonly amount: number;
  readonly note: string | null;
  readonly recordAsExpense: boolean;
}
