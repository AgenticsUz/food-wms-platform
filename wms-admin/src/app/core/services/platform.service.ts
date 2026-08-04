import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Tenant, CreateTenantDto, UpdateTenantDto, TenantModuleInfo,
  PaymentRecord, CreatePaymentDto, SuspendTenantDto
} from '../models/tenant.model';
import { Plan, CreatePlanDto, PlatformStats, ModuleInfo } from '../models/plan.model';

@Injectable({ providedIn: 'root' })
export class PlatformService {
  private api = inject(ApiService);

  // Tenants
  getTenants() { return this.api.get<Tenant[]>('admin/tenants'); }
  createTenant(dto: CreateTenantDto) { return this.api.post<Tenant>('admin/tenants', dto); }
  updateTenant(id: number, dto: UpdateTenantDto) { return this.api.put<Tenant>(`admin/tenants/${id}`, dto); }
  deleteTenant(id: number) { return this.api.delete<void>(`admin/tenants/${id}`); }
  suspendTenant(id: number, dto: SuspendTenantDto) { return this.api.put<Tenant>(`admin/tenants/${id}/suspend`, dto); }
  activateTenant(id: number) { return this.api.put<Tenant>(`admin/tenants/${id}/activate`, {}); }
  assignPlan(id: number, planId: number) { return this.api.put<Tenant>(`admin/tenants/${id}/plan`, { planId }); }
  getTenantModules(id: number) { return this.api.get<TenantModuleInfo[]>(`admin/tenants/${id}/modules`); }
  toggleModule(id: number, moduleId: number, isEnabled: boolean) {
    return this.api.put<void>(`admin/tenants/${id}/modules`, { moduleId, isEnabled });
  }

  // Payments (manual billing)
  getPayments(tenantId: number) { return this.api.get<PaymentRecord[]>(`admin/tenants/${tenantId}/payments`); }
  createPayment(tenantId: number, dto: CreatePaymentDto) {
    return this.api.post<PaymentRecord>(`admin/tenants/${tenantId}/payments`, dto);
  }
  deletePayment(paymentId: number) { return this.api.delete<void>(`admin/payments/${paymentId}`); }

  // Plans
  getPlans() { return this.api.get<Plan[]>('admin/plans'); }
  createPlan(dto: CreatePlanDto) { return this.api.post<Plan>('admin/plans', dto); }
  updatePlan(id: number, dto: CreatePlanDto) { return this.api.put<Plan>(`admin/plans/${id}`, dto); }
  deletePlan(id: number) { return this.api.delete<void>(`admin/plans/${id}`); }

  // Meta + stats
  getModules() { return this.api.get<ModuleInfo[]>('admin/modules'); }
  getStats() { return this.api.get<PlatformStats>('admin/stats'); }
}
