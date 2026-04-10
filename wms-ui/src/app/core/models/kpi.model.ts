export interface Shift {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  createdAt: string;
}

export interface ShiftCreateDto {
  name: string;
  startTime: string;
  endTime: string;
}

export interface ShiftPlan {
  id: number;
  shiftId: number;
  shiftName: string;
  productId: number;
  productName: string;
  plannedQuantity: number;
  date: string;
  createdAt: string;
}

export interface ShiftPlanCreateDto {
  shiftId: number;
  productId: number;
  plannedQuantity: number;
  date: string;
}

export interface ShiftActual {
  id: number;
  shiftId: number;
  shiftName: string;
  productId: number;
  productName: string;
  actualQuantity: number;
  wasteQuantity: number;
  date: string;
  note: string | null;
  createdAt: string;
}

export interface ShiftActualCreateDto {
  shiftId: number;
  productId: number;
  actualQuantity: number;
  wasteQuantity: number;
  date: string;
  note: string | null;
}

export interface AttendanceLog {
  id: number;
  userId: number;
  userName: string;
  shiftId: number;
  shiftName: string;
  checkIn: string;
  checkOut: string | null;
  method: AttendanceMethod;
  deviceId: string | null;
}

export enum AttendanceMethod {
  PIN = 1,
  FaceID = 2,
  Manual = 3
}

export interface CheckInDto {
  userId: number;
  shiftId: number;
  method: AttendanceMethod;
}
