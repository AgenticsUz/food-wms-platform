import { Routes } from '@angular/router';
import { featureGuard } from '../../core/guards/feature.guard';
import { permissionGuard } from '../../core/guards/permission.guard';

// Ruxsat kodlari backenddagi `RequirePermission` bilan bir xil. Ilgari bu yerda umuman
// tekshiruv yo'q edi: oddiy omborchi `/settings/users` manzilini qo'lda yozsa sahifa
// ochilib, keyin 403 xato toasti chiqardi.
const routes: Routes = [
  // Standart bo'lim — profil: uni har qanday foydalanuvchi ochadi. Ilgari `users` edi,
  // ya'ni ruxsatsiz odam `/settings` ga kirsa darrov taqiqqa urilardi.
  { path: '', redirectTo: 'profile', pathMatch: 'full' },
  { path: 'users', canActivate: [permissionGuard('settings.users')], loadComponent: () => import('./users/users.component') },
  { path: 'roles', canActivate: [permissionGuard('settings.roles')], loadComponent: () => import('./roles/roles.component') },
  { path: 'modules', canActivate: [permissionGuard('settings.modules')], loadComponent: () => import('./modules/modules.component') },
  { path: 'subscription', loadComponent: () => import('./subscription/subscription.component') },
  { path: 'qc-parameters', canActivate: [featureGuard('qc.parameters'), permissionGuard('quality.view')], loadComponent: () => import('./qc-parameters/qc-parameters.component') },
  { path: 'audit', canActivate: [permissionGuard('audit.view')], loadComponent: () => import('./audit-log/audit-log.component') },
  { path: 'profile', loadComponent: () => import('./profile/profile.component') }
];

export default routes;
