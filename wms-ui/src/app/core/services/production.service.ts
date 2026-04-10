import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  ProductionStage, ProductionStageCreateDto,
  ProductionRecipe, RecipeCreateDto,
  ProductionOrder, ProductionOrderCreateDto,
  StageExecuteDto
} from '../models/production.model';

@Injectable({ providedIn: 'root' })
export class ProductionService {
  private api = inject(ApiService);

  // Stages
  getStages() { return this.api.get<ProductionStage[]>('production/stages'); }
  createStage(dto: ProductionStageCreateDto) { return this.api.post<ProductionStage>('production/stages', dto); }
  updateStage(id: number, dto: ProductionStageCreateDto) { return this.api.put<ProductionStage>(`production/stages/${id}`, dto); }
  deleteStage(id: number) { return this.api.delete<void>(`production/stages/${id}`); }
  reorderStages(ids: number[]) { return this.api.patch<void>('production/stages/reorder', { ids }); }

  // Recipes
  getRecipes() { return this.api.get<ProductionRecipe[]>('production/recipes'); }
  getRecipe(id: number) { return this.api.get<ProductionRecipe>(`production/recipes/${id}`); }
  createRecipe(dto: RecipeCreateDto) { return this.api.post<ProductionRecipe>('production/recipes', dto); }
  updateRecipe(id: number, dto: RecipeCreateDto) { return this.api.put<ProductionRecipe>(`production/recipes/${id}`, dto); }
  deleteRecipe(id: number) { return this.api.delete<void>(`production/recipes/${id}`); }

  // Orders
  getOrders(params?: Record<string, string | number | boolean>) { return this.api.get<ProductionOrder[]>('production/orders', params); }
  getOrder(id: number) { return this.api.get<ProductionOrder>(`production/orders/${id}`); }
  createOrder(dto: ProductionOrderCreateDto) { return this.api.post<ProductionOrder>('production/orders', dto); }
  startOrder(id: number) { return this.api.put<ProductionOrder>(`production/orders/${id}/start`, {}); }
  completeOrder(id: number) { return this.api.put<ProductionOrder>(`production/orders/${id}/complete`, {}); }
  executeStage(orderId: number, stageId: number, dto: StageExecuteDto) {
    return this.api.put<void>(`production/orders/${orderId}/stages/${stageId}/execute`, dto);
  }
}
