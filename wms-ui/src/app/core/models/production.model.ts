export interface ProductionStage {
  id: number;
  name: string;
  orderNumber: number;
  description: string | null;
  createdAt: string;
}

export interface ProductionStageCreateDto {
  name: string;
  orderNumber: number;
  description: string | null;
}

export interface ProductionRecipe {
  id: number;
  outputProductId: number;
  outputProductName: string;
  name: string;
  outputQuantity: number;
  outputUnitId: number;
  outputUnitName: string;
  isActive: boolean;
  recipeStages: RecipeStage[];
  createdAt: string;
}

export interface RecipeStage {
  id: number;
  recipeId: number;
  stageId: number;
  stageName: string;
  orderNumber: number;
  outputProductId: number | null;
  outputProductName: string | null;
  expectedOutputQty: number | null;
  allowWarehouseOutput: boolean;
  outputWarehouseId: number | null;
  outputWarehouseName: string | null;
  inputs: RecipeStageItem[];
}

export interface RecipeStageItem {
  id?: number;
  recipeStageId?: number;
  productId: number;
  productName?: string;
  quantity: number;
  unitId: number;
  unitName?: string;
}

export interface RecipeCreateDto {
  outputProductId: number;
  name: string;
  outputQuantity: number;
  outputUnitId: number;
  isActive: boolean;
  stages: RecipeStageCreateDto[];
}

export interface RecipeStageCreateDto {
  stageId: number;
  orderNumber: number;
  outputProductId: number | null;
  expectedOutputQty: number | null;
  allowWarehouseOutput: boolean;
  outputWarehouseId: number | null;
  inputs: RecipeStageItemCreateDto[];
}

export interface RecipeStageItemCreateDto {
  productId: number;
  quantity: number;
  unitId: number;
}

export interface ProductionOrder {
  id: number;
  recipeId: number;
  recipeName: string;
  outputProductName: string;
  plannedQuantity: number;
  status: ProductionOrderStatus;
  plannedStartDate: string;
  plannedEndDate: string | null;
  assignedToUserId: number | null;
  assignedToUserName: string | null;
  note: string | null;
  stageExecutions: StageExecution[];
  createdAt: string;
}

export enum ProductionOrderStatus {
  Draft = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4
}

export interface ProductionOrderCreateDto {
  recipeId: number;
  plannedQuantity: number;
  plannedStartDate: string;
  plannedEndDate: string | null;
  assignedToUserId: number | null;
  note: string | null;
}

export interface StageExecution {
  id: number;
  productionOrderId: number;
  recipeStageId: number;
  stageName: string;
  orderNumber: number;
  plannedQuantity: number;
  actualQuantity: number;
  wasteQuantity: number;
  reworkQuantity: number;
  workerUserId: number | null;
  workerUserName: string | null;
  startTime: string | null;
  endTime: string | null;
  status: StageExecutionStatus;
  note: string | null;
}

export enum StageExecutionStatus {
  Pending = 1,
  InProgress = 2,
  Completed = 3,
  Skipped = 4
}

export interface StageExecuteDto {
  actualQuantity: number;
  wasteQuantity: number;
  reworkQuantity: number;
  note: string | null;
}
