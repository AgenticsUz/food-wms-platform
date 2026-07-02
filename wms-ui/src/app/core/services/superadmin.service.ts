import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Tenant, CreateTenantDto, UpdateTenantDto, TenantModuleInfo
} from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  private api = inject(ApiService);

  getTenants() { return this.api.get<Tenant[]>('tenants'); }
  createTenant(dto: CreateTenantDto) { return this.api.post<Tenant>('tenants', dto); }
  updateTenant(id: number, dto: UpdateTenantDto) { return this.api.put<Tenant>(`tenants/${id}`, dto); }
  deleteTenant(id: number) { return this.api.delete<void>(`tenants/${id}`); }

  getModules(tenantId: number) { return this.api.get<TenantModuleInfo[]>(`tenants/${tenantId}/modules`); }
  toggleModule(tenantId: number, moduleId: number, isEnabled: boolean) {
    return this.api.put<void>(`tenants/${tenantId}/modules`, { moduleId, isEnabled });
  }
}
