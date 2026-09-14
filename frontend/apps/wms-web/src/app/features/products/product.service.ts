import { Injectable, inject } from '@angular/core';

import { ApiService, type ApiCallOptions, type QueryParams } from '../../core/api/api.service';
import type {
  Category,
  CategoryCreateDto,
  Product,
  ProductCreateDto,
  Unit,
  UnitCreateDto,
} from './product.model';

/**
 * Backend `GET products` sukut bo'yicha 20 ta qaytaradi. Ro'yxat ekrani va
 * tanlov ro'yxatlari sahifalashni mijozda qiladi — shuning uchun hammasi
 * bitta so'rovda olinadi (eski ekran ham shunday qilardi). Transfer yaratish
 * esa eskisida parametrsiz chaqirilib, 21-mahsulotdan boshlab tanlovda
 * ko'rinmasdi — endi u ham shu parametrni ishlatadi.
 *
 * QIDIRUV endi bu ro'yxat ustida emas: `productSearch()` ni qarang.
 */
export const ALL_PRODUCTS: QueryParams = { page: 1, pageSize: 1000 };

/**
 * Qidiruv so'rovi parametrlari.
 *
 * NEGA server: mijozdagi `name.includes(q)` alifboni bilmaydi — «Сникерс» yozgan
 * odam «Snikers» ni topa olmasdi. Backend `search` ni pg_trgm va lotin↔kirill
 * transliteratsiya bilan bajaradi.
 *
 * `pageSize` ro'yxat ekranida ALL_PRODUCTS bilan bir xil qoladi: server
 * allaqachon FILTRLAB beradi, ya'ni qaytadigan hajm qidiruv natijasi qadar —
 * mijoz tomonda hech bir moslik yo'qolmasin.
 */
export function productSearch(query: string, pageSize = 1000): QueryParams {
  return { page: 1, pageSize, search: query };
}

/** Mahsulot, kategoriya va birliklar (eski `core/services/product.service`). */
@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly api = inject(ApiService);

  /**
   * `params` da `search` bo'lsa — server qidiruvi; bo'lmasa eski xatti-harakat
   * (sahifalangan ro'yxat). `options` — qidiruv fon so'rovi bo'lgani uchun
   * (`SEARCH_CALL`: `skipLoading`).
   */
  getProducts(params: QueryParams = ALL_PRODUCTS, options?: ApiCallOptions) {
    return this.api.get<Product[]>('products', params, options);
  }

  getProduct(id: string) {
    return this.api.get<Product>(`products/${id}`);
  }

  /**
   * Shtrix-kod bo'yicha SERVERDA qidirish. Kod yo'lda emas, so'rov satrida:
   * kodda `/` kabi belgilar bo'lishi mumkin (backend izohi).
   */
  getByBarcode(code: string, options?: ApiCallOptions) {
    return this.api.get<Product>('products/by-barcode', { code }, options);
  }

  createProduct(dto: ProductCreateDto) {
    return this.api.post<Product>('products', dto);
  }

  updateProduct(id: string, dto: ProductCreateDto) {
    return this.api.put<Product>(`products/${id}`, dto);
  }

  deleteProduct(id: string) {
    return this.api.delete<null>(`products/${id}`);
  }

  getCategories() {
    return this.api.get<Category[]>('categories');
  }

  createCategory(dto: CategoryCreateDto) {
    return this.api.post<Category>('categories', dto);
  }

  updateCategory(id: string, dto: CategoryCreateDto) {
    return this.api.put<Category>(`categories/${id}`, dto);
  }

  deleteCategory(id: string) {
    return this.api.delete<null>(`categories/${id}`);
  }

  getUnits() {
    return this.api.get<Unit[]>('units');
  }

  createUnit(dto: UnitCreateDto) {
    return this.api.post<Unit>('units', dto);
  }

  updateUnit(id: string, dto: UnitCreateDto) {
    return this.api.put<Unit>(`units/${id}`, dto);
  }

  deleteUnit(id: string) {
    return this.api.delete<null>(`units/${id}`);
  }
}
