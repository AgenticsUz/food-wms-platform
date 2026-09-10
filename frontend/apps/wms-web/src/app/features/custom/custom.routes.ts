import type { Routes } from '@angular/router';

import { featureGuard } from '../../core/auth/wms-guards';

/**
 * Bitta mijoz uchun yozilgan fitchalar (`docs/CUSTOM_FEATURES.md`). Har biri
 * `custom.<code>` feature'i bilan yopiladi — bu MAJBURIY.
 */
export const CUSTOM_ROUTES: Routes = [
  {
    path: 'example-feature',
    canActivate: [featureGuard('custom.example-feature')],
    data: { titleKey: 'custom.exampleFeature.title' },
    loadComponent: () => import('./example-feature/example-feature.component'),
  },
];
