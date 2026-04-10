export interface Product {
  id: number;
  name: string;
  categoryId: number;
  categoryName?: string;
  unitId: number;
  unitName?: string;
  unitShortName?: string;
  type: ProductType;
  minStock: number;
  shelfLifeDays: number | null;
  barcode: string | null;
  costPrice: number | null;
  createdAt: string;
  updatedAt: string;
}

export enum ProductType {
  Raw = 1,
  SemiFinished = 2,
  Finished = 3
}

export interface ProductCreateDto {
  name: string;
  categoryId: number;
  unitId: number;
  type: ProductType;
  minStock: number;
  shelfLifeDays: number | null;
  barcode: string | null;
  costPrice: number | null;
}

export interface Category {
  id: number;
  tenantId: number;
  name: string;
  parentId: number | null;
  children?: Category[];
  createdAt: string;
}

export interface CategoryCreateDto {
  name: string;
  parentId: number | null;
}

export interface Unit {
  id: number;
  name: string;
  shortName: string;
  createdAt: string;
}

export interface UnitCreateDto {
  name: string;
  shortName: string;
}
