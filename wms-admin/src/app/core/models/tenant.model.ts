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
  planId: number | null;
  planName: string | null;
  subscriptionStatus: SubscriptionStatus;
  trialEndsAt: string | null;
  createdAt: string;
  userCount: number;
}

export interface CreateTenantDto {
  name: string;
  slug: string;
  adminFullName: string;
  adminPhone: string;
  adminPassword: string;
  planId: number | null;
}

export interface UpdateTenantDto {
  name: string;
  slug: string;
  isActive: boolean;
  planId: number | null;
  subscriptionStatus: SubscriptionStatus;
  trialEndsAt: string | null;
}

export interface TenantModuleInfo {
  moduleId: number;
  moduleName: string;
  moduleCode: string;
  isEnabled: boolean;
}
