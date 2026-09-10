/**
 * Transfer modellari — manba backend `DTOs/Transfers/TransferDtos.cs`.
 *
 * Eski `wms-ui` modelidan farq:
 *  - id'lar Guid SATR. Transferning qisqa raqami YO'Q — UI `#12` o'rnida id'ning
 *    boshini ko'rsatadi (`shortTransferId`);
 *  - `typeName`/`statusName` backenddan kelmaydi — nomlar tarjima kalitidan
 *    (`transfer-enums.ts`), shunday bo'lgani ma'qul ham: server matni til
 *    almashganda eskirib qolardi;
 *  - yaratishda mahsulot qatori ixtiyoriy `locationId` ni ham qabul qiladi.
 */

export enum TransferType {
  Incoming = 1,
  Outgoing = 2,
  Internal = 3,
  ProductionOutput = 4,
  Return = 5,
}

export enum TransferStatus {
  Pending = 1,
  Confirmed = 2,
  Rejected = 3,
  Cancelled = 4,
}

export enum ReturnReason {
  Expired = 1,
  Unsold = 2,
  Defective = 3,
  Other = 4,
}

export interface TransferItem {
  readonly id: string;
  readonly productId: string;
  readonly productName: string;
  readonly unitShortName: string;
  readonly batchId: string | null;
  readonly lotNumber: string | null;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly totalPrice: number;
}

export interface Transfer {
  readonly id: string;
  readonly type: TransferType;
  readonly status: TransferStatus;
  readonly fromWarehouseId: string | null;
  readonly fromWarehouseName: string | null;
  readonly toWarehouseId: string | null;
  readonly toWarehouseName: string | null;
  readonly counterpartyId: string | null;
  readonly counterpartyName: string | null;
  readonly agentId: string | null;
  readonly agentName: string | null;
  readonly commissionPercent: number | null;
  readonly createdByUserId: string | null;
  readonly createdByUserName: string | null;
  readonly returnReason: ReturnReason | null;
  readonly returnReasonName: string | null;
  readonly originalTransferId: string | null;
  readonly note: string | null;
  readonly confirmedAt: string | null;
  readonly createdAt: string;
  /** Massiv `readonly` emas: PrimeNG `p-table [value]` o'zgaruvchan massiv turini kutadi. */
  readonly items: TransferItem[];
  readonly totalAmount: number;
}

/** Yaratish so'rovidagi mahsulot qatori (`CreateTransferItemDto`). */
export interface TransferItemDto {
  readonly productId: string;
  readonly batchId: string | null;
  readonly locationId?: string | null;
  readonly quantity: number;
  readonly unitPrice: number;
}

export interface TransferCreateDto {
  readonly type: TransferType;
  readonly fromWarehouseId: string | null;
  readonly toWarehouseId: string | null;
  readonly counterpartyId: string | null;
  readonly agentId: string | null;
  readonly commissionPercent: number | null;
  readonly returnReason: ReturnReason | null;
  readonly originalTransferId: string | null;
  readonly note: string | null;
  readonly items: readonly TransferItemDto[];
}
