/**
 * Kontragent modellari — manba `src/WMS.Application/DTOs/Counterparties/CounterpartyDtos.cs`.
 *
 * F6 da O'CHDI: `portalPhone`/`portalEnabled`/`portalPassword` (kontragent portali, D8)
 * va `organizationId`/`isPlatformTenant` (global INN katalogi, D10). INN oddiy maydon
 * bo'lib qoldi. `createdAt`/`updatedAt` ham DTO'da yo'q.
 */

export enum CounterpartyType {
  Supplier = 1,
  Client = 2,
  Both = 3,
}

export interface Counterparty {
  readonly id: string;
  readonly name: string;
  readonly type: CounterpartyType;
  readonly phone: string | null;
  readonly address: string | null;
  readonly note: string | null;
  /** STIR — 9 raqam. Faqat saqlanadi, avtomatik bog'lash yo'q (D10). */
  readonly inn: string | null;
  readonly agentId: string | null;
  readonly agentName: string | null;
}

/** `CreateCounterpartyDto` va `UpdateCounterpartyDto` — shakli bir xil. */
export interface CounterpartySaveDto {
  readonly name: string;
  readonly type: CounterpartyType;
  readonly phone: string | null;
  readonly inn: string | null;
  readonly address: string | null;
  readonly note: string | null;
  readonly agentId: string | null;
}

export interface CounterpartyBalance {
  readonly counterpartyId: string;
  readonly counterpartyName: string;
  readonly debtAmount: number;
}

/** Tur nomi kaliti — sarlavha va qarzlar jadvali uchun. */
export function counterpartyTypeKey(type: CounterpartyType): string {
  switch (type) {
    case CounterpartyType.Supplier:
      return 'partners.typeSupplier';
    case CounterpartyType.Client:
      return 'partners.typeClient';
    default:
      return 'partners.typeBoth';
  }
}

// ── O'tkazma (MINIMAL, mahalliy) ──
// `transfers` bo'limi hali ko'chirilmagan; kontragent sahifasidagi tarix jadvaliga
// kerakli maydonlar `TransferDto` dan olingan. Enum qiymatlari — `WMS.Domain.Enums`.

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

/**
 * Kontragent kartasidagi o'tkazma qatori — manba umumiy `GET /api/transfers`
 * javobi (`TransferDto`), shuning uchun tovar qatorlari (`items`) ham shu yerda:
 * «mijozga aynan nima yuborilgan» savoli qo'shimcha so'rovsiz yopiladi.
 */
export interface CounterpartyTransfer {
  readonly id: string;
  readonly type: TransferType;
  readonly status: TransferStatus;
  readonly totalAmount: number;
  readonly createdAt: string;
  /** Tasdiqlangan payt; tasdiqlanmagan hujjatda `null` — u holda `createdAt`. */
  readonly confirmedAt: string | null;
  readonly fromWarehouseName: string | null;
  readonly toWarehouseName: string | null;
  readonly note: string | null;
  readonly items: readonly CounterpartyTransferItem[];
}

/** `TransferItemDto` ning kartada ko'rsatiladigan qismi. */
export interface CounterpartyTransferItem {
  readonly id: string;
  readonly productName: string;
  readonly unitShortName: string;
  readonly lotNumber: string | null;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly totalPrice: number;
}

/** `StatusBadgeComponent` CSS kaliti (eski `transfer-enums.ts`). */
export function transferStatusClass(status: TransferStatus): string {
  switch (status) {
    case TransferStatus.Pending:
      return 'Pending';
    case TransferStatus.Confirmed:
      return 'Confirmed';
    case TransferStatus.Rejected:
      return 'Rejected';
    case TransferStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Neutral';
  }
}

export function transferStatusKey(status: TransferStatus): string {
  switch (status) {
    case TransferStatus.Pending:
      return 'status.pending';
    case TransferStatus.Confirmed:
      return 'status.confirmed';
    case TransferStatus.Rejected:
      return 'status.rejected';
    case TransferStatus.Cancelled:
      return 'status.cancelled';
    default:
      return 'status.ok';
  }
}

export function transferTypeClass(type: TransferType): string {
  switch (type) {
    case TransferType.Incoming:
      return 'Incoming';
    case TransferType.Outgoing:
      return 'Outgoing';
    case TransferType.Internal:
      return 'Internal';
    case TransferType.ProductionOutput:
      return 'Production';
    case TransferType.Return:
      return 'Return';
    default:
      return 'Neutral';
  }
}

export function transferTypeKey(type: TransferType): string {
  switch (type) {
    case TransferType.Incoming:
      return 'transfer.incoming';
    case TransferType.Outgoing:
      return 'transfer.outgoing';
    case TransferType.Internal:
      return 'transfer.internal';
    case TransferType.ProductionOutput:
      return 'transfer.production';
    case TransferType.Return:
      return 'transfer.return';
    default:
      return 'transfer.transfers';
  }
}
