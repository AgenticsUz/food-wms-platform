/**
 * Mahsulot katalogi modellari — manba backend `DTOs/Products/ProductDtos.cs`.
 *
 * Eski `wms-ui` modelidan farq:
 *  - id'lar Guid SATR (eski `number`);
 *  - `createdAt`/`updatedAt` (mahsulot), `tenantId`/`createdAt` (kategoriya) va
 *    `createdAt` (birlik) backend DTO'sida YO'Q — modeldan ham olib tashlandi:
 *    TS'da qolsa shablon jimgina bo'sh katak ko'rsatardi, kompilyator esa ushlamasdi.
 *
 * `enum` lar `/api/*` da SON bo'lib keladi (backendda `JsonStringEnumConverter` yo'q).
 */

export enum ProductType {
  Raw = 1,
  SemiFinished = 2,
  Finished = 3,
}

export interface Product {
  readonly id: string;
  readonly name: string;
  readonly categoryId: string;
  readonly categoryName: string;
  readonly unitId: string;
  readonly unitName: string;
  readonly unitShortName: string;
  readonly type: ProductType;
  readonly minStock: number;
  readonly shelfLifeDays: number | null;
  readonly barcode: string | null;
  readonly costPrice: number | null;
}

/** `CreateProductDto` va `UpdateProductDto` backendda bir xil shaklda. */
export interface ProductCreateDto {
  readonly name: string;
  readonly categoryId: string;
  readonly unitId: string;
  readonly type: ProductType;
  readonly minStock: number;
  readonly shelfLifeDays: number | null;
  readonly barcode: string | null;
  readonly costPrice: number | null;
}

export interface Category {
  readonly id: string;
  readonly name: string;
  readonly parentId: string | null;
  readonly children: readonly Category[];
}

export interface CategoryCreateDto {
  readonly name: string;
  readonly parentId: string | null;
}

export interface Unit {
  readonly id: string;
  readonly name: string;
  readonly shortName: string;
}

export interface UnitCreateDto {
  readonly name: string;
  readonly shortName: string;
}
