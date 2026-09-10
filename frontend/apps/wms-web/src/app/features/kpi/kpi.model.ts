/**
 * KPI modeli — backend `WMS.Application/DTOs/Kpi/KpiDtos.cs` va
 * `DTOs/Analytics/AnalyticsDtos.cs` bilan bir xil. Id'lar Guid satr.
 *
 * F6 da `ShiftDto`, `ShiftPlanDto`, `ShiftActualDto` dan `createdAt` O'CHDI —
 * ekranlardagi «yaratilgan sana» ustunlari shu sababli olib tashlandi.
 */

export interface Shift {
  readonly id: string;
  readonly name: string;
  /** .NET `TimeSpan` — `"08:00:00"`. */
  readonly startTime: string;
  readonly endTime: string;
}

export interface ShiftCreateDto {
  readonly name: string;
  readonly startTime: string;
  readonly endTime: string;
}

export interface ShiftPlan {
  readonly id: string;
  readonly shiftId: string;
  readonly shiftName: string;
  readonly productId: string;
  readonly productName: string;
  readonly plannedQuantity: number;
  readonly date: string;
}

export interface ShiftPlanCreateDto {
  readonly shiftId: string;
  readonly productId: string;
  readonly plannedQuantity: number;
  /** Faqat sana — `YYYY-MM-DD` (mahalliy kun). */
  readonly date: string;
}

export interface ShiftActual {
  readonly id: string;
  readonly shiftId: string;
  readonly shiftName: string;
  readonly productId: string;
  readonly productName: string;
  readonly actualQuantity: number;
  readonly wasteQuantity: number;
  readonly date: string;
  readonly note: string | null;
}

export interface ShiftActualCreateDto {
  readonly shiftId: string;
  readonly productId: string;
  readonly actualQuantity: number;
  readonly wasteQuantity: number;
  readonly date: string;
  readonly note: string | null;
}

/** Backend enum'lari raqam bo'lib keladi (WMS API'da `JsonStringEnumConverter` yo'q). */
export enum AttendanceMethod {
  PIN = 1,
  FaceID = 2,
  Manual = 3,
}

export interface AttendanceLog {
  readonly id: string;
  /** `user_profile.id` (Identity `sub` EMAS). */
  readonly userId: string;
  readonly userName: string;
  readonly shiftId: string;
  readonly shiftName: string;
  readonly checkIn: string;
  readonly checkOut: string | null;
  readonly method: AttendanceMethod;
  readonly deviceId: string | null;
}

export interface CheckInDto {
  readonly userId: string;
  readonly shiftId: string;
  readonly method: AttendanceMethod;
}

/** `GET analytics/kpi/shift-efficiency`. */
export interface ShiftEfficiencyDto {
  readonly shiftName: string;
  readonly date: string;
  readonly plannedQty: number;
  readonly actualQty: number;
  readonly efficiencyPercent: number;
}

/** `GET analytics/production/plan-vs-actual`. */
export interface PlanVsActualDto {
  readonly date: string;
  readonly productName: string;
  readonly planned: number;
  readonly actual: number;
  readonly efficiencyPercent: number;
}

/**
 * Tanlov ro'yxatlari uchun qisqa shakllar. To'liq `product.model` boshqa bo'limniki
 * (parallel ko'chirilmoqda) — unga bog'lanmaslik uchun faqat kerakli maydonlar.
 */
export interface ProductOption {
  readonly id: string;
  readonly name: string;
}

export interface WorkerOption {
  readonly id: string;
  readonly fullName: string;
}
