import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TenantService } from '../services/tenant.service';

export const moduleGuard = (moduleCode: string): CanActivateFn => {
  return () => {
    const tenantService = inject(TenantService);
    const router = inject(Router);

    if (tenantService.isModuleEnabled(moduleCode)) {
      return true;
    }

    router.navigate(['/dashboard']);
    return false;
  };
};
