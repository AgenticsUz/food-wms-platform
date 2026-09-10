import { Injectable, inject } from '@angular/core';
import { forkJoin, map, of, type Observable } from 'rxjs';

import { ApiService, type QueryParams } from '../../core/api/api.service';
import type { Transfer } from '../transfers/transfer.model';
import type {
  Batch,
  Location,
  LocationCreateDto,
  UpdateBatchDto,
  Warehouse,
  WarehouseCreateDto,
  WarehouseStockDetail,
  WarehouseStockGrouped,
  WarehouseStockRow,
} from './warehouse.model';

/** Omborlar, qoldiq, joylar, partiyalar (eski `core/services/warehouse.service`). */
@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly api = inject(ApiService);

  getWarehouses() {
    return this.api.get<Warehouse[]>('warehouses');
  }

  createWarehouse(dto: WarehouseCreateDto) {
    return this.api.post<Warehouse>('warehouses', dto);
  }

  updateWarehouse(id: string, dto: WarehouseCreateDto) {
    return this.api.put<Warehouse>(`warehouses/${id}`, dto);
  }

  deleteWarehouse(id: string) {
    return this.api.delete<null>(`warehouses/${id}`);
  }

  getStock(warehouseId: string) {
    return this.api.get<WarehouseStockGrouped[]>(`warehouses/${warehouseId}/stock`);
  }

  /** Bitta ombor qoldig'i, har qatorga ombor nomi qo'shilgan. */
  getStockRows(warehouse: Warehouse): Observable<WarehouseStockRow[]> {
    return this.getStock(warehouse.id).pipe(
      map((res) =>
        res.success && res.data
          ? res.data.map((s) => ({ ...s, warehouseId: warehouse.id, warehouseName: warehouse.name }))
          : []
      )
    );
  }

  /**
   * Barcha omborlar qoldig'i. Backendda «hamma ombor» so'rovi yo'q — har ombor
   * alohida so'raladi va birlashtiriladi (eski ekran bilan bir xil).
   */
  getAllStockRows(warehouses: readonly Warehouse[]): Observable<WarehouseStockRow[]> {
    if (warehouses.length === 0) return of([]);
    return forkJoin(warehouses.map((w) => this.getStockRows(w))).pipe(map((rows) => rows.flat()));
  }

  getStockDetail(warehouseId: string) {
    return this.api.get<WarehouseStockDetail[]>(`warehouses/${warehouseId}/stock/detail`);
  }

  /**
   * Harakatlar — alohida endpoint yo'q: tasdiqlangan transferlar olinadi,
   * komponent ularni mahsulot qatorlariga yoyadi.
   */
  getMovements(params?: QueryParams) {
    return this.api.get<Transfer[]>('transfers', params);
  }

  getLocations(params?: QueryParams) {
    return this.api.get<Location[]>('locations', params);
  }

  createLocation(dto: LocationCreateDto) {
    return this.api.post<Location>('locations', dto);
  }

  updateLocation(id: string, dto: LocationCreateDto) {
    return this.api.put<Location>(`locations/${id}`, dto);
  }

  deleteLocation(id: string) {
    return this.api.delete<null>(`locations/${id}`);
  }

  getBatches() {
    return this.api.get<Batch[]>('batches');
  }

  updateBatch(id: string, dto: UpdateBatchDto) {
    return this.api.put<Batch>(`batches/${id}`, dto);
  }

  deleteBatch(id: string) {
    return this.api.delete<null>(`batches/${id}`);
  }
}
