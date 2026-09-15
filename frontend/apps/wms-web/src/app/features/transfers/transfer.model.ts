/**
 * Transfer modellari — manba backend `DTOs/Transfers/TransferDtos.cs`.
 *
 * Eski `wms-ui` modelidan farq:
 *  - id'lar Guid SATR, LEKIN hujjatning tenant ichidagi qisqa raqami bor
 *    (`number`, P2.4) — ekranda `#12` aynan shu, Guid faqat tooltipda qoladi;
 *  - `documentDate` — HUJJAT sanasi (kalendar kuni), `createdAt` esa yozuv
 *    yaratilgan lahza: hujjat kechagi kun bilan kiritilishi mumkin, shuning
 *    uchun ro'yxat, filtr va hisobot `documentDate` bo'yicha ishlaydi;
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

/**
 * Hujjat qaysi yuzadan kirgan (`WMS.Domain/Enums/DocumentSource.cs`).
 *
 * ⚠️ Yaratish DTO'sida YO'Q: manbani SERVER qo'yadi — mijoz o'z hujjatini «AI
 * yozgan» deb ko'rsatolmasin.
 */
export enum DocumentSource {
  Ui = 1,
  Telegram = 2,
  Ai = 3,
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
  /** Tenant ichidagi qisqa raqam (1 dan) — ekranda ko'rinadigan «hujjat raqami». */
  readonly number: number;
  /**
   * Hujjat sanasi (kalendar kuni, serverda UTC yarim tuni). Ekranda AYNAN shu
   * «sana» — `createdAt` audit izi bo'lib qoladi.
   */
  readonly documentDate: string;
  /** Hujjat qaysi yuzadan kirgan — AI/Telegram yozuvi belgisi uchun. */
  readonly source: DocumentSource;
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

/**
 * «Oxirgi narx» taklifi (`GET pricing/last-price`, `LastPriceDto`).
 *
 * `isSameCounterparty` — taklifning ISHONCH darajasi: shu kontragent bilan
 * bo'lgan narx u bilan KELISHILGAN narx, boshqasiniki esa faqat orientir.
 * Shuning uchun UI ikkalasini bir xil ko'rsatmaydi.
 */
export interface LastPrice {
  readonly unitPrice: number;
  readonly documentDate: string;
  readonly transferId: string;
  readonly number: number;
  readonly counterpartyId: string | null;
  readonly counterpartyName: string | null;
  readonly isSameCounterparty: boolean;
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
  /**
   * Hujjat sanasi — `YYYY-MM-DD` (mahalliy kalendar kuni, `toLocalDateString`).
   * Bo'sh — bugun; kelajak sana 400; o'tgan sana `documents.backdate` ruxsati
   * bilan (`DocumentDates.Resolve` qoidasi, to'lovlar bilan bir xil).
   */
  readonly documentDate: string | null;
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
