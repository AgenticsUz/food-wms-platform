export interface Plan {
  id: number;
  name: string;
  code: string;
  price: number;
  isActive: boolean;
  moduleCodes: string[];
  maxUsers: number;
  maxWarehouses: number;
  maxTransfersPerMonth: number;
  tenantCount: number;
}

export interface CreatePlanDto {
  name: string;
  code: string;
  price: number;
  isActive: boolean;
  moduleCodes: string[];
  maxUsers: number;
  maxWarehouses: number;
  maxTransfersPerMonth: number;
}

export interface PlatformStats {
  totalTenants: number;
  activeTenants: number;
  trialTenants: number;
  suspendedTenants: number;
  totalUsers: number;
  newTenantsThisMonth: number;
  monthlyGrowth: { month: string; count: number }[];
  recentTenants: { id: number; name: string; slug: string; subscriptionStatus: number; createdAt: string }[];
}

export interface ModuleInfo {
  moduleId: number;
  moduleName: string;
  moduleCode: string;
}
