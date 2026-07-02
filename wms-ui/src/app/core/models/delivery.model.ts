export enum DeliveryStatus {
  Planned = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4
}

export enum DeliveryStopStatus {
  Pending = 1,
  Delivered = 2,
  Failed = 3
}

export interface Vehicle {
  id: number;
  name: string;
  model: string | null;
  capacity: number;
  isActive: boolean;
}

export interface CreateVehicleDto {
  name: string;
  model: string | null;
  capacity: number;
  isActive: boolean;
}

export interface Driver {
  id: number;
  fullName: string;
  phone: string | null;
  licenseNumber: string | null;
  isActive: boolean;
}

export interface CreateDriverDto {
  fullName: string;
  phone: string | null;
  licenseNumber: string | null;
  isActive: boolean;
}

export interface DeliveryStop {
  id: number;
  counterpartyId: number;
  counterpartyName: string | null;
  transferId: number | null;
  address: string | null;
  sequenceOrder: number;
  status: DeliveryStopStatus;
  deliveredAt: string | null;
  note: string | null;
}

export interface Delivery {
  id: number;
  vehicleId: number | null;
  vehicleName: string | null;
  driverId: number | null;
  driverName: string | null;
  status: DeliveryStatus;
  scheduledDate: string;
  note: string | null;
  createdByUserName: string | null;
  createdAt: string;
  stops: DeliveryStop[];
  stopCount: number;
  deliveredCount: number;
}

export interface CreateDeliveryStopDto {
  counterpartyId: number;
  transferId: number | null;
  address: string | null;
  sequenceOrder: number;
  note: string | null;
}

export interface CreateDeliveryDto {
  vehicleId: number | null;
  driverId: number | null;
  scheduledDate: string;
  note: string | null;
  stops: CreateDeliveryStopDto[];
}
