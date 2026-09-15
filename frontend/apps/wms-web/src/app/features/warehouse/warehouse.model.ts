/**
 * Ombor modellari — manba backend `DTOs/Warehouses/WarehouseDtos.cs`.
 *
 * Eski `wms-ui` modelidan farq: id'lar Guid SATR; `createdAt` (ombor, joy,
 * partiya) backendda yo'q. Batafsil qoldiq (`StockDetailDto`) endi o'z qatori
 * id'sini va ombor nomini bermaydi — so'rov allaqachon bitta omborga tegishli.
 */

export enum WarehouseType {
  Raw = 1,
  Finished = 2,
  General = 3,
}

export interface Warehouse {
  readonly id: string;
  readonly name: string;
  readonly type: WarehouseType;
  readonly description: string | null;
}

export interface WarehouseCreateDto {
  readonly name: string;
  readonly type: WarehouseType;
  readonly description: string | null;
}

export interface Location {
  readonly id: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly name: string;
  readonly code: string | null;
}

export interface LocationCreateDto {
  readonly warehouseId: string;
  readonly name: string;
  readonly code: string | null;
}

export interface Batch {
  readonly id: string;
  readonly productId: string;
  readonly productName: string;
  readonly lotNumber: string;
  readonly manufacturedDate: string;
  readonly expiryDate: string | null;
  readonly initialQuantity: number;
  readonly remainingQuantity: number;
  readonly notes: string | null;
}

export interface UpdateBatchDto {
  readonly lotNumber: string;
  /** Faqat sana (`YYYY-MM-DD`, mahalliy kun) — vaqt nuqtasi emas. */
  readonly expiryDate: string | null;
  readonly notes: string | null;
}

/** `GET warehouses/{id}/stock` — mahsulot bo'yicha yig'ilgan qoldiq. */
export interface WarehouseStockGrouped {
  readonly productId: string;
  readonly productName: string;
  readonly unitShortName: string;
  readonly totalQuantity: number;
  readonly reservedQuantity: number;
  readonly availableQuantity: number;
}

/** `GET warehouses/{id}/stock/detail` — partiya va joy bo'yicha. */
export interface WarehouseStockDetail {
  readonly productId: string;
  readonly productName: string;
  readonly unitShortName: string;
  readonly batchId: string;
  readonly lotNumber: string;
  readonly expiryDate: string | null;
  readonly locationId: string;
  readonly locationName: string;
  readonly quantity: number;
  readonly reservedQuantity: number;
}

/** Zaxira ko'rinishi qatori: yig'ilgan qoldiq + qaysi omborniki. */
export interface WarehouseStockRow extends WarehouseStockGrouped {
  readonly warehouseId: string;
  readonly warehouseName: string;
}

/**
 * Standart ombor sozlamasi (P2.6) — manba `DTOs/Warehouses/WarehouseDefaultsDto.cs`.
 *
 * ⚠️ FORMA FAQAT `effective*` GA QARAYDI. Ustunlik tartibini (xodimning shaxsiy
 * tanlovi → tenant sozlamasi → tenantda ombor bitta bo'lsa o'sha) server hisoblab
 * beradi; uni mijozda takrorlash ikki joyda ikki xil natija degani bo'lardi.
 * `rawWarehouseId` / `finishedWarehouseId` / `userWarehouseId` faqat SOZLAMALAR
 * ekraniga — «hozir nima yozilgan» ni ko'rsatish uchun.
 */
export interface WarehouseDefaults {
  readonly rawWarehouseId: string | null;
  readonly finishedWarehouseId: string | null;
  readonly userWarehouseId: string | null;
  readonly effectiveRawId: string | null;
  readonly effectiveFinishedId: string | null;
}

/**
 * `PUT /api/settings/warehouses` — tenant sozlamasi, ALMASHTIRISH semantikasi:
 * ikkala maydon ham yuboriladi, `null` sozlamani o'chiradi.
 */
export interface WarehouseDefaultsUpdateDto {
  readonly rawWarehouseId: string | null;
  readonly finishedWarehouseId: string | null;
}
