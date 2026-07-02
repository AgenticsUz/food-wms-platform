import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';

const routes: Routes = [
  { path: '', canActivate: [permissionGuard('delivery.view')], loadComponent: () => import('./deliveries/deliveries.component') },
  { path: 'new', canActivate: [permissionGuard('delivery.manage')], loadComponent: () => import('./delivery-create/delivery-create.component') },
  { path: 'vehicles', canActivate: [permissionGuard('delivery.view')], loadComponent: () => import('./vehicles/vehicles.component') },
  { path: 'drivers', canActivate: [permissionGuard('delivery.view')], loadComponent: () => import('./drivers/drivers.component') },
  { path: ':id', canActivate: [permissionGuard('delivery.view')], loadComponent: () => import('./delivery-detail/delivery-detail.component') }
];

export default routes;
