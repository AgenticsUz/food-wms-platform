import { Injectable, inject } from '@angular/core';

import type { TelegramLinkApi } from '../../shared/components/telegram-link-dialog/telegram-link-dialog.component';
import type { TelegramLinkToken, TelegramSubjectLink } from '../settings/settings.model';
import { ApiService } from '../../core/api/api.service';
import type {
  ClientOption,
  CreateDeliveryDto,
  CreateDriverDto,
  CreateVehicleDto,
  Delivery,
  DeliveryStatus,
  Driver,
  Vehicle,
} from './delivery.model';

/** Yetkazish bo'limi API'si (eski `core/services/delivery.service.ts`). */
@Injectable({ providedIn: 'root' })
export class DeliveryService {
  private readonly api = inject(ApiService);

  getVehicles() {
    return this.api.get<Vehicle[]>('delivery/vehicles');
  }
  createVehicle(dto: CreateVehicleDto) {
    return this.api.post<Vehicle>('delivery/vehicles', dto);
  }
  updateVehicle(id: string, dto: CreateVehicleDto) {
    return this.api.put<Vehicle>(`delivery/vehicles/${id}`, dto);
  }
  deleteVehicle(id: string) {
    return this.api.delete<void>(`delivery/vehicles/${id}`);
  }

  getDrivers() {
    return this.api.get<Driver[]>('delivery/drivers');
  }
  createDriver(dto: CreateDriverDto) {
    return this.api.post<Driver>('delivery/drivers', dto);
  }
  /** Haydovchi Telegram'i (TG12) — kartadan havola, menejer haydovchiga o'zi yuboradi. */
  driverTelegram(id: string): TelegramLinkApi {
    return {
      get: () => this.api.get<TelegramSubjectLink>(`delivery/drivers/${id}/telegram`),
      create: () => this.api.post<TelegramLinkToken>(`delivery/drivers/${id}/telegram-link`, {}),
      unlink: () => this.api.delete<void>(`delivery/drivers/${id}/telegram`),
    };
  }
  updateDriver(id: string, dto: CreateDriverDto) {
    return this.api.put<Driver>(`delivery/drivers/${id}`, dto);
  }
  deleteDriver(id: string) {
    return this.api.delete<void>(`delivery/drivers/${id}`);
  }

  getDeliveries(status?: DeliveryStatus) {
    return this.api.get<Delivery[]>('delivery', { status });
  }
  getDelivery(id: string) {
    return this.api.get<Delivery>(`delivery/${id}`);
  }
  createDelivery(dto: CreateDeliveryDto) {
    return this.api.post<Delivery>('delivery', dto);
  }
  updateStatus(id: string, status: DeliveryStatus) {
    return this.api.put<Delivery>(`delivery/${id}/status`, { status });
  }

  markDelivered(id: string, stopId: string) {
    return this.api.put<Delivery>(`delivery/${id}/stops/${stopId}/deliver`, {});
  }
  markFailed(id: string, stopId: string, note: string | null) {
    return this.api.put<Delivery>(`delivery/${id}/stops/${stopId}/fail`, { note });
  }

  waybillPath(id: string): string {
    return `delivery/${id}/waybill`;
  }

  /** Manzil uchun hamkorlar (`partners.view` ruxsati kerak — backend `CounterpartiesController`). */
  getCounterparties() {
    return this.api.get<ClientOption[]>('counterparties');
  }
}
