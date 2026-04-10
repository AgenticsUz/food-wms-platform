import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { TenantService } from '../services/tenant.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const tenantService = inject(TenantService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    if (tenantService.enabledModules().length === 0) {
      tenantService.restoreModules();
    }
    return true;
  }

  router.navigate(['/auth/login']);
  return false;
};
