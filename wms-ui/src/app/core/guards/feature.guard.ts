import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { FeatureService } from '../services/feature.service';

/**
 * `moduleGuard` bilan bir xil uslub, faqat mayda donada.
 * Haqiqiy cheklov backendda (403 `feature_disabled:CODE`) — bu qatlam UX uchun.
 */
export const featureGuard = (featureCode: string): CanActivateFn => {
  return () => {
    const features = inject(FeatureService);
    const router = inject(Router);
    if (features.isEnabled(featureCode)) return true;
    router.navigate(['/dashboard']);
    return false;
  };
};
