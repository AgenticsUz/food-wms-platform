/**
 * Kabinet modellari — manba `src/WMS.Application/DTOs/Portal/PortalDtos.cs`.
 *
 * ⚠️ Ilovaning `Transfer` modelidan ATAYLAB ajratilgan: kabinet javobida ombor,
 * hujjatni kim yaratgani va agent foizi YO'Q (ichki ma'lumot). Ikkalasi bitta
 * tur bo'lsa, yuzaga qo'shilgan maydon tasodifan mijozga ham ko'rinardi.
 */

import { TransferStatus, TransferType } from '../transfers/transfer.model';

export enum PortalActorKind {
  Counterparty = 1,
  Agent = 2,
}

export enum PortalCounterpartyType {
  Supplier = 1,
  Client = 2,
  Both = 3,
}

export interface PortalMe {
  readonly id: string;
  readonly kind: PortalActorKind;
  readonly name: string;
  readonly counterpartyType: PortalCounterpartyType | null;
  readonly phone: string | null;
  readonly inn: string | null;
  readonly tenantName: string;
}

export interface PortalFinance {
  /** Musbat — men qarzdorman; manfiy — zavod menga qarzdor. */
  readonly debtAmount: number;
  readonly totalTurnover: number;
  readonly totalPaid: number;
  readonly lastPaymentAt: string | null;
}

export interface PortalTransferItem {
  readonly id: string;
  readonly productName: string;
  readonly unitShortName: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly totalPrice: number;
}

export interface PortalTransfer {
  readonly id: string;
  readonly type: TransferType;
  readonly status: TransferStatus;
  readonly totalAmount: number;
  readonly note: string | null;
  readonly createdAt: string;
  readonly confirmedAt: string | null;
  /**
   * Hujjat sanasi (P2.3) — tovar HAQIQATDA kelgan/ketgan KUN, vaqt nuqtasi emas.
   * Kabinet ro'yxati ham shu bo'yicha tartiblangan, ya'ni zavoddagi tartib bilan
   * bir xil. ⚠️ Soatsiz ko'rsatiladi: kun boshi bo'lgani uchun har yozuvda
   * «00:00» chiqardi.
   */
  readonly documentDate: string;
  /** Qisqa hujjat raqami (P2.4) — mijoz telefonda «12-hujjat» deb ayta olsin. */
  readonly number: number;
  readonly items: PortalTransferItem[];
}

export interface PortalPayment {
  readonly id: string;
  readonly amount: number;
  readonly method: number;
  /**
   * To'lov sanasi — endi HUJJAT sanasi (kun boshi), operator kiritgan lahza emas.
   * Shu sababli soatsiz ko'rsatiladi.
   */
  readonly paidAt: string;
  readonly note: string | null;
}

export interface PortalAgentSummary {
  readonly commissionPercent: number;
  readonly clientCount: number;
  readonly totalSales: number;
  readonly commissionEarned: number;
  readonly commissionPaid: number;
  readonly commissionPending: number;
}

export interface PortalAgentClient {
  readonly id: string;
  readonly name: string;
  readonly phone: string | null;
  readonly debtAmount: number;
}

/** Kontragent/agent kartasidagi kabinet hisobi (admin yuzasi). */
export interface PortalAccount {
  readonly enabled: boolean;
  readonly identitySub: string | null;
}
