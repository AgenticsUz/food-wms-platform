import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { authGuard } from './core/guards/auth.guard';
import { moduleGuard } from './core/guards/module.guard';
import { portalGuard } from './core/guards/portal.guard';
import { agentPortalGuard } from './core/guards/agent-portal.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { superAdminGuard } from './core/guards/superadmin.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./modules/auth/auth.routes')
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./modules/dashboard/dashboard.component')
      },
      {
        path: 'warehouse',
        canActivate: [moduleGuard('WAREHOUSE_RAW')],
        loadChildren: () => import('./modules/warehouse/warehouse.routes')
      },
      {
        path: 'production',
        canActivate: [moduleGuard('PRODUCTION')],
        loadChildren: () => import('./modules/production/production.routes')
      },
      {
        path: 'transfers',
        canActivate: [moduleGuard('TRANSFERS')],
        loadChildren: () => import('./modules/transfers/transfers.routes')
      },
      {
        path: 'finance',
        canActivate: [moduleGuard('FINANCE'), permissionGuard('finance.view')],
        loadChildren: () => import('./modules/finance/finance.routes')
      },
      {
        path: 'kpi',
        canActivate: [moduleGuard('KPI'), permissionGuard('kpi.view')],
        loadChildren: () => import('./modules/kpi/kpi.routes')
      },
      {
        path: 'counterparties',
        loadChildren: () => import('./modules/counterparties/counterparties.routes')
      },
      {
        path: 'agents',
        canActivate: [permissionGuard('agents.view')],
        loadChildren: () => import('./modules/agents/agents.routes')
      },
      {
        path: 'products',
        loadChildren: () => import('./modules/products/products.routes')
      },
      {
        path: 'settings',
        loadChildren: () => import('./modules/settings/settings.routes')
      },
      {
        path: 'superadmin/tenants',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./modules/superadmin/tenants/superadmin-tenants.component')
      }
    ]
  },
  {
    path: 'portal',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./modules/portal/login/portal-login.component')
      },
      {
        path: '',
        canActivate: [portalGuard],
        loadComponent: () => import('./modules/portal/layout/portal-layout.component'),
        children: [
          { path: '', loadChildren: () => import('./modules/portal/portal.routes') }
        ]
      }
    ]
  },
  {
    path: 'agent-portal',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./modules/agent-portal/login/agent-portal-login.component')
      },
      {
        path: '',
        canActivate: [agentPortalGuard],
        loadComponent: () => import('./modules/agent-portal/layout/agent-portal-layout.component'),
        children: [
          { path: '', loadChildren: () => import('./modules/agent-portal/agent-portal.routes') }
        ]
      }
    ]
  }
];
