import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from '../services/permission.service';

export const permissionGuard = (code: string): CanActivateFn => () => {
  const perm = inject(PermissionService);
  if (perm.can(code)) return true;
  inject(Router).navigate(['/dashboard']);
  return false;
};
