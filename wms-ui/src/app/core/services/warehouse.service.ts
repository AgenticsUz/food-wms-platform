import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Warehouse,
  WarehouseCreateDto,
  Location,
  LocationCreateDto,
  Batch,
  WarehouseStock,
  StockMovement
} from '../models/warehouse.model';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private api = inject(ApiService);

  // Warehouses
  getWarehouses() {
    return this.api.get<Warehouse[]>('warehouses');
  }

  createWarehouse(dto: WarehouseCreateDto) {
    return this.api.post<Warehouse>('warehouses', dto);
  }

  updateWarehouse(id: number, dto: WarehouseCreateDto) {
    return this.api.put<Warehouse>(`warehouses/${id}`, dto);
  }

  // Stock
  getStock(warehouseId: number) {
    return this.api.get<WarehouseStock[]>(`warehouses/${warehouseId}/stock/detail`);
  }

  getAllStock() {
    return this.api.get<WarehouseStock[]>('warehouses/0/stock/detail');
  }

  // Movements
  getMovements(params?: Record<string, string | number | boolean>) {
    return this.api.get<StockMovement[]>('transfers', params);
  }

  // Locations
  getLocations(params?: Record<string, string | number | boolean>) {
    return this.api.get<Location[]>('locations', params);
  }

  createLocation(dto: LocationCreateDto) {
    return this.api.post<Location>('locations', dto);
  }

  updateLocation(id: number, dto: LocationCreateDto) {
    return this.api.put<Location>(`locations/${id}`, dto);
  }

  deleteLocation(id: number) {
    return this.api.delete<void>(`locations/${id}`);
  }

  // Batches
  getBatches(params?: Record<string, string | number | boolean>) {
    return this.api.get<Batch[]>('batches', params);
  }
}
