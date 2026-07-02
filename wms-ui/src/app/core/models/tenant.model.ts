export enum SubscriptionStatus {
  Trial = 1,
  Active = 2,
  Suspended = 3
}

export interface Tenant {
  id: number;
  name: string;
  slug: string;
  isActive: boolean;
  planType: string | null;
  subscriptionStatus: SubscriptionStatus;
  createdAt: string;
  userCount: number;
}

export interface CreateTenantDto {
  name: string;
  slug: string;
  adminFullName: string;
  adminPhone: string;
  adminPassword: string;
}

export interface UpdateTenantDto {
  name: string;
  slug: string;
  isActive: boolean;
  planType: string | null;
  subscriptionStatus: SubscriptionStatus;
}

export interface TenantModuleInfo {
  moduleId: number;
  moduleName: string;
  moduleCode: string;
  isEnabled: boolean;
}
