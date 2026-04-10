import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, of, map } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/api-response.model';
import {
  Warehouse,
  WarehouseCreateDto,
  Location,
  LocationCreateDto,
  Batch,
  WarehouseStockGrouped,
  WarehouseStockDetail,
  WarehouseStockRow,
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

  // Stock — grouped by product for a single warehouse
  getStock(warehouseId: number): Observable<ApiResponse<WarehouseStockGrouped[]>> {
    return this.api.get<WarehouseStockGrouped[]>(`warehouses/${warehouseId}/stock`);
  }

  // Stock — grouped, enriched with warehouse name, for a single warehouse
  getStockRows(warehouse: Warehouse): Observable<WarehouseStockRow[]> {
    return this.getStock(warehouse.id).pipe(
      map(res => {
        if (!res.success || !res.data) return [];
        return res.data.map(s => ({
          ...s,
          warehouseId: warehouse.id,
          warehouseName: warehouse.name
        }));
      })
    );
  }

  // Stock — load from ALL warehouses and merge
  getAllStockRows(warehouses: Warehouse[]): Observable<WarehouseStockRow[]> {
    if (warehouses.length === 0) return of([]);
    return forkJoin(warehouses.map(w => this.getStockRows(w))).pipe(
      map(arrays => arrays.flat())
    );
  }

  // Detailed stock (by batch/location)
  getStockDetail(warehouseId: number) {
    return this.api.get<WarehouseStockDetail[]>(`warehouses/${warehouseId}/stock/detail`);
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
