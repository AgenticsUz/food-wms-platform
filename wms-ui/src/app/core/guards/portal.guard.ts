import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const portalGuard: CanActivateFn = () => {
  const router = inject(Router);
  const portalToken = localStorage.getItem('portalToken');

  if (portalToken) {
    return true;
  }

  router.navigate(['/portal/login']);
  return false;
};
