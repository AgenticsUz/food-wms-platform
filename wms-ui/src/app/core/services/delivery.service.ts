import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Vehicle, CreateVehicleDto, Driver, CreateDriverDto,
  Delivery, CreateDeliveryDto, DeliveryStatus
} from '../models/delivery.model';

@Injectable({ providedIn: 'root' })
export class DeliveryService {
  private api = inject(ApiService);

  // Vehicles
  getVehicles() { return this.api.get<Vehicle[]>('delivery/vehicles'); }
  createVehicle(dto: CreateVehicleDto) { return this.api.post<Vehicle>('delivery/vehicles', dto); }
  updateVehicle(id: number, dto: CreateVehicleDto) { return this.api.put<Vehicle>(`delivery/vehicles/${id}`, dto); }
  deleteVehicle(id: number) { return this.api.delete<void>(`delivery/vehicles/${id}`); }

  // Drivers
  getDrivers() { return this.api.get<Driver[]>('delivery/drivers'); }
  createDriver(dto: CreateDriverDto) { return this.api.post<Driver>('delivery/drivers', dto); }
  updateDriver(id: number, dto: CreateDriverDto) { return this.api.put<Driver>(`delivery/drivers/${id}`, dto); }
  deleteDriver(id: number) { return this.api.delete<void>(`delivery/drivers/${id}`); }

  // Deliveries
  getDeliveries(status?: DeliveryStatus) {
    return this.api.get<Delivery[]>('delivery', status ? { status } : {});
  }
  getDelivery(id: number) { return this.api.get<Delivery>(`delivery/${id}`); }
  createDelivery(dto: CreateDeliveryDto) { return this.api.post<Delivery>('delivery', dto); }
  updateStatus(id: number, status: DeliveryStatus) { return this.api.put<Delivery>(`delivery/${id}/status`, { status }); }
  deleteDelivery(id: number) { return this.api.delete<void>(`delivery/${id}`); }

  markDelivered(id: number, stopId: number) { return this.api.put<void>(`delivery/${id}/stops/${stopId}/deliver`, {}); }
  markFailed(id: number, stopId: number, note: string | null) { return this.api.put<void>(`delivery/${id}/stops/${stopId}/fail`, { note }); }

  waybillUrl(id: number) { return `delivery/${id}/waybill`; }
}
