import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Product,
  ProductCreateDto,
  Category,
  CategoryCreateDto,
  Unit,
  UnitCreateDto
} from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private api = inject(ApiService);

  // Products
  getProducts(params?: Record<string, string | number | boolean>) {
    return this.api.get<Product[]>('products', params);
  }

  getProduct(id: number) {
    return this.api.get<Product>(`products/${id}`);
  }

  createProduct(dto: ProductCreateDto) {
    return this.api.post<Product>('products', dto);
  }

  updateProduct(id: number, dto: ProductCreateDto) {
    return this.api.put<Product>(`products/${id}`, dto);
  }

  deleteProduct(id: number) {
    return this.api.delete<void>(`products/${id}`);
  }

  // Categories
  getCategories() {
    return this.api.get<Category[]>('categories');
  }

  createCategory(dto: CategoryCreateDto) {
    return this.api.post<Category>('categories', dto);
  }

  updateCategory(id: number, dto: CategoryCreateDto) {
    return this.api.put<Category>(`categories/${id}`, dto);
  }

  deleteCategory(id: number) {
    return this.api.delete<void>(`categories/${id}`);
  }

  // Units
  getUnits() {
    return this.api.get<Unit[]>('units');
  }

  createUnit(dto: UnitCreateDto) {
    return this.api.post<Unit>('units', dto);
  }

  updateUnit(id: number, dto: UnitCreateDto) {
    return this.api.put<Unit>(`units/${id}`, dto);
  }

  deleteUnit(id: number) {
    return this.api.delete<void>(`units/${id}`);
  }
}
