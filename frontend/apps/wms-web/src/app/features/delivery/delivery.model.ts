/**
 * Yetkazish modeli — backend `WMS.Application/DTOs/Delivery/DeliveryDtos.cs` bilan
 * bir xil. Id'lar Guid satr; enum'lar raqam bo'lib keladi.
 */

export enum DeliveryStatus {
  Planned = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4,
}

export enum DeliveryStopStatus {
  Pending = 1,
  Delivered = 2,
  Failed = 3,
}

export interface Vehicle {
  readonly id: string;
  readonly name: string;
  readonly model: string | null;
  readonly capacity: number;
  readonly isActive: boolean;
}

export interface CreateVehicleDto {
  readonly name: string;
  readonly model: string | null;
  readonly capacity: number;
  readonly isActive: boolean;
}

export interface Driver {
  readonly id: string;
  readonly fullName: string;
  readonly phone: string | null;
  readonly licenseNumber: string | null;
  readonly isActive: boolean;
}

export interface CreateDriverDto {
  readonly fullName: string;
  readonly phone: string | null;
  readonly licenseNumber: string | null;
  readonly isActive: boolean;
}

export interface DeliveryStop {
  readonly id: string;
  readonly counterpartyId: string;
  readonly counterpartyName: string | null;
  readonly transferId: string | null;
  readonly address: string | null;
  readonly sequenceOrder: number;
  readonly status: DeliveryStopStatus;
  readonly deliveredAt: string | null;
  readonly note: string | null;
}

export interface Delivery {
  readonly id: string;
  readonly vehicleId: string | null;
  readonly vehicleName: string | null;
  readonly driverId: string | null;
  readonly driverName: string | null;
  readonly status: DeliveryStatus;
  readonly scheduledDate: string;
  readonly note: string | null;
  readonly createdByUserName: string | null;
  readonly createdAt: string;
  /** Mutable massiv: `p-table [value]` `readonly` massivni qabul qilmaydi. */
  readonly stops: DeliveryStop[];
  readonly stopCount: number;
  readonly deliveredCount: number;
}

export interface CreateDeliveryStopDto {
  readonly counterpartyId: string;
  readonly transferId: string | null;
  readonly address: string | null;
  readonly sequenceOrder: number;
  readonly note: string | null;
}

export interface CreateDeliveryDto {
  readonly vehicleId: string | null;
  readonly driverId: string | null;
  /** Faqat sana — `YYYY-MM-DD` (mahalliy kun). */
  readonly scheduledDate: string;
  readonly note: string | null;
  readonly stops: readonly CreateDeliveryStopDto[];
}

/** Backend `CounterpartyType`: 1 — ta'minotchi, 2 — mijoz, 3 — ikkalasi. */
export const COUNTERPARTY_CLIENT = 2;
export const COUNTERPARTY_BOTH = 3;

/**
 * Manzil tanlovi uchun hamkorning qisqa shakli. To'liq `counterparty.model`
 * boshqa bo'limniki (parallel ko'chirilmoqda) — unga bog'lanilmaydi.
 */
export interface ClientOption {
  readonly id: string;
  readonly name: string;
  readonly type: number;
}

/** Guid ro'yxatda uzun — foydalanuvchiga birinchi 8 belgi yetarli (to'liq qiymat `title` da). */
export function shortId(id: string): string {
  // Guid v7 ning boshi — vaqt belgisi (bir vaqtdagi yozuvlarda bir xil); farqlovchi qism — oxiri.
  return id.length > 8 ? id.slice(-8) : id;
}
