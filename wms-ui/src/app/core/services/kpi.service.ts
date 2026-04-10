import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Shift, ShiftCreateDto,
  ShiftPlan, ShiftPlanCreateDto,
  ShiftActual, ShiftActualCreateDto,
  AttendanceLog, CheckInDto
} from '../models/kpi.model';
import { ShiftEfficiencyDto, PlanVsActualDto } from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class KpiService {
  private api = inject(ApiService);

  // Shifts
  getShifts() { return this.api.get<Shift[]>('shifts'); }
  createShift(dto: ShiftCreateDto) { return this.api.post<Shift>('shifts', dto); }
  updateShift(id: number, dto: ShiftCreateDto) { return this.api.put<Shift>(`shifts/${id}`, dto); }
  deleteShift(id: number) { return this.api.delete<void>(`shifts/${id}`); }

  // Plans
  getPlans(params?: Record<string, string | number | boolean>) { return this.api.get<ShiftPlan[]>('kpi/plans', params); }
  createPlan(dto: ShiftPlanCreateDto) { return this.api.post<ShiftPlan>('kpi/plans', dto); }

  // Actuals
  getActuals(params?: Record<string, string | number | boolean>) { return this.api.get<ShiftActual[]>('kpi/actuals', params); }
  createActual(dto: ShiftActualCreateDto) { return this.api.post<ShiftActual>('kpi/actuals', dto); }

  // Analytics
  getEfficiency(days = 7) { return this.api.get<ShiftEfficiencyDto[]>('analytics/kpi/shift-efficiency', { days }); }
  getPlanVsActual(days = 7) { return this.api.get<PlanVsActualDto[]>('analytics/production/plan-vs-actual', { days }); }

  // Attendance
  getAttendance(params?: Record<string, string | number | boolean>) { return this.api.get<AttendanceLog[]>('attendance', params); }
  checkIn(dto: CheckInDto) { return this.api.post<AttendanceLog>('attendance/checkin', dto); }
  checkOut(id: number) { return this.api.put<AttendanceLog>(`attendance/${id}/checkout`, {}); }
}
