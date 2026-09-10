import { Injectable, inject } from '@angular/core';

import { ApiService } from '../../core/api/api.service';
import type {
  AttendanceLog,
  CheckInDto,
  PlanVsActualDto,
  ProductOption,
  Shift,
  ShiftActual,
  ShiftActualCreateDto,
  ShiftCreateDto,
  ShiftEfficiencyDto,
  ShiftPlan,
  ShiftPlanCreateDto,
  WorkerOption,
} from './kpi.model';

/**
 * Tanlov ro'yxati uchun mahsulotlar soni. Backend `products` sahifalaydi
 * (standart 20 ta) — eski ilova parametrsiz chaqirib, 21-mahsulotdan boshlab
 * rejaga qo'shib bo'lmas edi.
 */
const PRODUCT_LOOKUP_SIZE = 1000;

/** KPI bo'limi API'si (eski `core/services/kpi.service.ts`). */
@Injectable({ providedIn: 'root' })
export class KpiService {
  private readonly api = inject(ApiService);

  getShifts() {
    return this.api.get<Shift[]>('shifts');
  }
  createShift(dto: ShiftCreateDto) {
    return this.api.post<Shift>('shifts', dto);
  }
  updateShift(id: string, dto: ShiftCreateDto) {
    return this.api.put<Shift>(`shifts/${id}`, dto);
  }
  deleteShift(id: string) {
    return this.api.delete<void>(`shifts/${id}`);
  }

  getPlans() {
    return this.api.get<ShiftPlan[]>('kpi/plans');
  }
  createPlan(dto: ShiftPlanCreateDto) {
    return this.api.post<ShiftPlan>('kpi/plans', dto);
  }

  getActuals() {
    return this.api.get<ShiftActual[]>('kpi/actuals');
  }
  createActual(dto: ShiftActualCreateDto) {
    return this.api.post<ShiftActual>('kpi/actuals', dto);
  }

  getEfficiency(days = 7) {
    return this.api.get<ShiftEfficiencyDto[]>('analytics/kpi/shift-efficiency', { days });
  }
  getPlanVsActual(days = 7) {
    return this.api.get<PlanVsActualDto[]>('analytics/production/plan-vs-actual', { days });
  }

  getAttendance() {
    return this.api.get<AttendanceLog[]>('attendance');
  }
  checkIn(dto: CheckInDto) {
    return this.api.post<AttendanceLog>('attendance/checkin', dto);
  }
  checkOut(id: string) {
    return this.api.put<AttendanceLog>(`attendance/${id}/checkout`, {});
  }

  getProducts() {
    return this.api.get<ProductOption[]>('products', { page: 1, pageSize: PRODUCT_LOOKUP_SIZE });
  }

  /**
   * Ishchilar ro'yxati — `GET users` (`settings.users` ruxsati). Davomat
   * kirituvchida bu ruxsat bo'lmasligi mumkin: toast chiqmaydi, ro'yxat bo'sh
   * qoladi (backend'da KPI uchun alohida ishchilar so'rovi yo'q).
   */
  getWorkers() {
    return this.api.get<WorkerOption[]>('users', undefined, { skipErrorNotify: true });
  }
}
