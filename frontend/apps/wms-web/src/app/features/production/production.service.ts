import { Injectable, inject } from '@angular/core';

import { ApiService, type QueryParams } from '../../core/api/api.service';
import type {
  ProductOption,
  ProductionOrder,
  ProductionOrderCreateDto,
  ProductionRecipe,
  ProductionStage,
  ProductionStageSaveDto,
  RecipeCreateDto,
  StageExecuteDto,
  StageExecution,
  UnitOption,
  UserOption,
  WarehouseOption,
  WasteByStage,
} from './production.model';

/**
 * Ishlab chiqarish API'si (eski `core/services/production.service.ts`). Yo'llar
 * o'zgarmagan — `ProductionController` (`api/production`).
 */
@Injectable({ providedIn: 'root' })
export class ProductionService {
  private readonly api = inject(ApiService);

  // Bosqichlar
  getStages() {
    return this.api.get<ProductionStage[]>('production/stages');
  }
  createStage(dto: ProductionStageSaveDto) {
    return this.api.post<ProductionStage>('production/stages', dto);
  }
  updateStage(id: string, dto: ProductionStageSaveDto) {
    return this.api.put<ProductionStage>(`production/stages/${id}`, dto);
  }
  deleteStage(id: string) {
    return this.api.delete<null>(`production/stages/${id}`);
  }
  reorderStages(ids: readonly string[]) {
    return this.api.patch<null>('production/stages/reorder', { ids });
  }

  // Retseptlar
  getRecipes() {
    return this.api.get<ProductionRecipe[]>('production/recipes');
  }
  getRecipe(id: string) {
    return this.api.get<ProductionRecipe>(`production/recipes/${id}`);
  }
  createRecipe(dto: RecipeCreateDto) {
    return this.api.post<ProductionRecipe>('production/recipes', dto);
  }
  deleteRecipe(id: string) {
    return this.api.delete<null>(`production/recipes/${id}`);
  }

  // Buyurtmalar
  getOrders(params?: QueryParams) {
    return this.api.get<ProductionOrder[]>('production/orders', params);
  }
  getOrder(id: string) {
    return this.api.get<ProductionOrder>(`production/orders/${id}`);
  }
  createOrder(dto: ProductionOrderCreateDto) {
    return this.api.post<ProductionOrder>('production/orders', dto);
  }
  startOrder(id: string) {
    return this.api.put<ProductionOrder>(`production/orders/${id}/start`, {});
  }
  completeOrder(id: string) {
    return this.api.put<ProductionOrder>(`production/orders/${id}/complete`, {});
  }
  executeStage(orderId: string, stageId: string, dto: StageExecuteDto) {
    return this.api.put<StageExecution>(`production/orders/${orderId}/stages/${stageId}/execute`, dto);
  }

  /**
   * Brak grafigi — `analytics.advanced` feature'i ortida. Grafik ixtiyoriy
   * bezak: rad etilsa sahifada qizil toast chiqmasin (`skipErrorNotify`).
   */
  getWasteByStage(days = 30) {
    return this.api.get<WasteByStage[]>(
      'analytics/production/waste-by-stage',
      { days },
      { skipErrorNotify: true }
    );
  }

  // ── Tanlov ro'yxatlari (boshqa bo'limlarning endpoint'lari) ──

  /**
   * `GET products` sahifalangan (standart `pageSize=20`). Tanlov ro'yxati uchun
   * hammasi kerak — aks holda 21-mahsulot retseptga qo'shib bo'lmas edi.
   */
  getProducts() {
    return this.api.get<ProductOption[]>('products', { page: 1, pageSize: 1000 });
  }
  getUnits() {
    return this.api.get<UnitOption[]>('units');
  }
  /** Ixtiyoriy ma'lumot (ombor nomi) — ruxsat bo'lmasa ekran baribir ishlaydi. */
  getWarehouses() {
    return this.api.get<WarehouseOption[]>('warehouses', undefined, { skipErrorNotify: true });
  }
  /**
   * `GET users` `settings.users` ruxsatini talab qiladi — ishlab chiqarish
   * menejerida u bo'lmasligi mumkin. «Tayinlash» maydoni ixtiyoriy, shuning
   * uchun 403 jim o'tkaziladi va ro'yxat bo'sh qoladi.
   */
  getUsers() {
    return this.api.get<UserOption[]>('users', undefined, { skipErrorNotify: true });
  }
}
