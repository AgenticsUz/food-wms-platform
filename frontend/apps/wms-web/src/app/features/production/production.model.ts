/**
 * Ishlab chiqarish modellari — manba `src/WMS.Application/DTOs/Production/ProductionDtos.cs`.
 *
 * F6 dagi farqlar (eski `wms-ui/core/models/production.model.ts` ga nisbatan):
 *  - hamma id — Guid SATR;
 *  - retseptda bosqichlar `stages` (eskisida `recipeStages`) — nomi DTO bo'yicha;
 *  - `createdAt` (bosqichda), `recipeId`/`outputWarehouseName` (retsept bosqichida),
 *    `productionOrderId` (bosqich bajarilishida) backend DTO'sida YO'Q;
 *  - retsept yaratish DTO'sida `isActive` YO'Q — backend uni qabul qilmaydi.
 */

export interface ProductionStage {
  readonly id: string;
  readonly name: string;
  readonly orderNumber: number;
  readonly description: string | null;
}

/** `CreateProductionStageDto` va `UpdateProductionStageDto` — shakli bir xil. */
export interface ProductionStageSaveDto {
  readonly name: string;
  readonly orderNumber: number;
  readonly description: string | null;
}

export interface ProductionRecipe {
  readonly id: string;
  readonly name: string;
  readonly outputProductId: string;
  readonly outputProductName: string;
  readonly outputQuantity: number;
  readonly outputUnitId: string;
  readonly outputUnitName: string;
  readonly isActive: boolean;
  readonly stages: readonly RecipeStage[];
}

export interface RecipeStage {
  readonly id: string;
  readonly stageId: string;
  readonly stageName: string;
  readonly orderNumber: number;
  readonly outputProductId: string | null;
  readonly outputProductName: string | null;
  readonly expectedOutputQty: number | null;
  readonly allowWarehouseOutput: boolean;
  readonly outputWarehouseId: string | null;
  readonly inputs: readonly RecipeStageItem[];
}

export interface RecipeStageItem {
  readonly id: string;
  readonly productId: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitId: string;
  readonly unitName: string;
}

export interface RecipeCreateDto {
  readonly name: string;
  readonly outputProductId: string;
  readonly outputQuantity: number;
  readonly outputUnitId: string;
  readonly stages: readonly RecipeStageCreateDto[];
}

export interface RecipeStageCreateDto {
  readonly stageId: string;
  readonly orderNumber: number;
  readonly outputProductId: string | null;
  readonly expectedOutputQty: number | null;
  readonly allowWarehouseOutput: boolean;
  readonly outputWarehouseId: string | null;
  readonly inputs: readonly RecipeStageItemCreateDto[];
}

export interface RecipeStageItemCreateDto {
  readonly productId: string;
  readonly quantity: number;
  readonly unitId: string;
}

export enum ProductionOrderStatus {
  Draft = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4,
}

export interface ProductionOrder {
  readonly id: string;
  readonly recipeId: string;
  readonly recipeName: string;
  readonly outputProductName: string;
  readonly plannedQuantity: number;
  readonly status: ProductionOrderStatus;
  readonly plannedStartDate: string;
  readonly plannedEndDate: string | null;
  readonly assignedToUserId: string | null;
  readonly assignedToUserName: string | null;
  readonly note: string | null;
  readonly createdAt: string;
  readonly stageExecutions: readonly StageExecution[];
}

export interface ProductionOrderCreateDto {
  readonly recipeId: string;
  readonly plannedQuantity: number;
  /** Faqat sana (`YYYY-MM-DD`, mahalliy kun) — `toLocalDateString`. */
  readonly plannedStartDate: string;
  readonly plannedEndDate: string | null;
  readonly assignedToUserId: string | null;
  readonly note: string | null;
}

export enum StageExecutionStatus {
  Pending = 1,
  InProgress = 2,
  Completed = 3,
  Skipped = 4,
}

export interface StageExecution {
  readonly id: string;
  readonly recipeStageId: string;
  readonly stageName: string;
  readonly orderNumber: number;
  readonly plannedQuantity: number;
  readonly actualQuantity: number;
  readonly wasteQuantity: number;
  readonly reworkQuantity: number;
  readonly workerUserId: string | null;
  readonly workerUserName: string | null;
  readonly startTime: string | null;
  readonly endTime: string | null;
  readonly status: StageExecutionStatus;
  readonly note: string | null;
}

export interface StageExecuteDto {
  readonly actualQuantity: number;
  readonly wasteQuantity: number;
  readonly reworkQuantity: number;
  readonly note: string | null;
}

/** `GET analytics/production/waste-by-stage` (`AnalyticsDtos.cs` → `WasteByStageDto`). */
export interface WasteByStage {
  readonly stageName: string;
  readonly totalActual: number;
  readonly totalWaste: number;
  readonly wastePercent: number;
}

// ── Boshqa bo'limlarning modellari (MINIMAL, mahalliy) ──
// `products`, `warehouse`, `settings/users` bo'limlari hali ko'chirilmagan — ularning
// modeli paydo bo'lganda shu turlar o'shalarning importiga almashtiriladi. Faqat
// ekranda ishlatiladigan maydonlar olingan (manba — backend DTO'lari).

/** `ProductDto` dan: tanlov ro'yxati uchun. */
export interface ProductOption {
  readonly id: string;
  readonly name: string;
}

/** `UnitDto` dan. */
export interface UnitOption {
  readonly id: string;
  readonly name: string;
  readonly shortName: string;
}

/** `WarehouseDto` dan. */
export interface WarehouseOption {
  readonly id: string;
  readonly name: string;
}

/** `UserDto` dan (`GET users`). */
export interface UserOption {
  readonly id: string;
  readonly fullName: string;
}

/**
 * Buyurtma/o'tkazmaning qisqa ko'rinishi. Eski UI `#15` kabi butun son ko'rsatardi;
 * F6 da id — Guid va qisqa raqam YO'Q, 36 belgili Guid jadvalga sig'maydi. Birinchi
 * 8 belgi odam ko'zi bilan ajratish uchun yetarli (to'qnashuv ehtimoli amalda nol).
 */
export function shortId(id: string): string {
  // Guid v7 ning BOSHI — vaqt belgisi: bir vaqtda yaratilgan yozuvlarda bir xil chiqadi
  // (stendda hamma buyurtma `#01a08c84` edi). Farqlovchi qism — OXIRI.
  return id.slice(-8);
}
