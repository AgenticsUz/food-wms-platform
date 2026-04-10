export interface Warehouse {
  id: number;
  name: string;
  type: WarehouseType;
  description: string | null;
  createdAt: string;
}

export enum WarehouseType {
  Raw = 1,
  Finished = 2,
  General = 3
}

export interface WarehouseCreateDto {
  name: string;
  type: WarehouseType;
  description: string | null;
}

export interface Location {
  id: number;
  warehouseId: number;
  warehouseName?: string;
  name: string;
  code: string | null;
  createdAt: string;
}

export interface LocationCreateDto {
  warehouseId: number;
  name: string;
  code: string | null;
}

export interface Batch {
  id: number;
  productId: number;
  productName: string;
  lotNumber: string;
  manufacturedDate: string;
  expiryDate: string | null;
  initialQuantity: number;
  remainingQuantity: number;
  createdAt: string;
}

export interface WarehouseStock {
  id: number;
  warehouseId: number;
  warehouseName: string;
  locationId: number;
  locationName: string;
  productId: number;
  productName: string;
  batchId: number;
  lotNumber: string;
  expiryDate: string | null;
  quantity: number;
  reservedQuantity: number;
  unitShortName: string;
}

export interface StockMovement {
  id: number;
  productName: string;
  warehouseName: string;
  type: string;
  quantity: number;
  unitShortName: string;
  date: string;
  note: string | null;
}
