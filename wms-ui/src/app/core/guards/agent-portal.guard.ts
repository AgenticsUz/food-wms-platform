import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const agentPortalGuard: CanActivateFn = () => {
  const router = inject(Router);
  const token = localStorage.getItem('agentPortalToken');

  if (token) {
    return true;
  }

  router.navigate(['/agent-portal/login']);
  return false;
};
