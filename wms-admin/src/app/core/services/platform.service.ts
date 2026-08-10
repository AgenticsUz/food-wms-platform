import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Tenant, CreateTenantDto, UpdateTenantDto, TenantModuleInfo,
  PaymentRecord, CreatePaymentDto, SuspendTenantDto, ExpiringTenant,
  TenantUser, ResetUserPasswordDto, PasswordResetResult
} from '../models/tenant.model';
import { Plan, CreatePlanDto, PlatformStats, ModuleInfo } from '../models/plan.model';
import { Lead, UpdateLeadDto, ConvertLeadDto } from '../models/lead.model';
import { FeatureInfo, TenantFeature, UpdateFeaturesDto } from '../models/feature.model';
import { Organization, OrganizationDetail } from '../models/organization.model';
import { AuditQuery, PaginatedAudit } from '../models/audit.model';

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

  // Users and password recovery
  // Mijoz o'z tizimidan qulflanib qolganda qaytishning yagona yo'li — shu ikki endpoint.
  getTenantUsers(tenantId: number) { return this.api.get<TenantUser[]>(`admin/tenants/${tenantId}/users`); }
  /** Javobdagi `newPassword` bir martalik — uni loglash yoki toast'ga chiqarish mumkin emas. */
  resetUserPassword(tenantId: number, dto: ResetUserPasswordDto) {
    return this.api.post<PasswordResetResult>(`admin/tenants/${tenantId}/reset-user-password`, dto);
  }

  // Payments (manual billing)
  getPayments(tenantId: number) { return this.api.get<PaymentRecord[]>(`admin/tenants/${tenantId}/payments`); }
  createPayment(tenantId: number, dto: CreatePaymentDto) {
    return this.api.post<PaymentRecord>(`admin/tenants/${tenantId}/payments`, dto);
  }
  deletePayment(paymentId: number) { return this.api.delete<void>(`admin/payments/${paymentId}`); }
  /** Muddati `days` ichida tugaydiganlar — allaqachon o'tganlar ham shu javobda (manfiy `daysLeft`). */
  getExpiring(days = 7) { return this.api.get<ExpiringTenant[]>('admin/tenants/expiring', { days }); }

  // Leads (demo so'rovlari)
  getLeads() { return this.api.get<Lead[] | { items: Lead[] }>('admin/leads', { page: 1, pageSize: 200 }); }
  getLead(id: number) { return this.api.get<Lead>(`admin/leads/${id}`); }
  updateLead(id: number, dto: UpdateLeadDto) { return this.api.put<Lead>(`admin/leads/${id}`, dto); }
  convertLead(id: number, dto: ConvertLeadDto) { return this.api.post<Tenant>(`admin/leads/${id}/convert`, dto); }

  // Plans
  getPlans() { return this.api.get<Plan[]>('admin/plans'); }
  createPlan(dto: CreatePlanDto) { return this.api.post<Plan>('admin/plans', dto); }
  updatePlan(id: number, dto: CreatePlanDto) { return this.api.put<Plan>(`admin/plans/${id}`, dto); }
  deletePlan(id: number) { return this.api.delete<void>(`admin/plans/${id}`); }

  // Organizations (platforma darajasidagi kompaniyalar)
  getOrganizations(search?: string) {
    return this.api.get<Organization[] | { items: Organization[] }>('admin/organizations',
      search ? { search, page: 1, pageSize: 200 } : { page: 1, pageSize: 200 });
  }
  getOrganization(id: number) { return this.api.get<OrganizationDetail>(`admin/organizations/${id}`); }

  // Features (menyu darajasidagi boshqaruv)
  getFeatures() { return this.api.get<FeatureInfo[]>('admin/features'); }
  getTenantFeatures(tenantId: number) { return this.api.get<TenantFeature[]>(`admin/tenants/${tenantId}/features`); }
  updateTenantFeatures(tenantId: number, dto: UpdateFeaturesDto) {
    return this.api.put<void>(`admin/tenants/${tenantId}/features`, dto);
  }

  // Audit (platforma ko'rinishi, B4)
  /** Sahifalash server tomonda — `pageSize` 200 dan oshsa backend jimgina qisqartiradi. */
  getAudit(query: AuditQuery) {
    const params: Record<string, string | number | boolean> = {
      page: query.page, pageSize: query.pageSize
    };
    if (query.tenantId) params['tenantId'] = query.tenantId;
    if (query.entityType) params['entityType'] = query.entityType;
    if (query.action) params['action'] = query.action;
    if (query.platformOnly) params['platformOnly'] = true;
    if (query.from) params['from'] = query.from;
    if (query.to) params['to'] = query.to;
    return this.api.get<PaginatedAudit>('admin/audit', params);
  }

  getAuditEntityTypes() { return this.api.get<string[]>('admin/audit/entity-types'); }

  // Meta + stats
  getModules() { return this.api.get<ModuleInfo[]>('admin/modules'); }
  getStats() { return this.api.get<PlatformStats>('admin/stats'); }
}
