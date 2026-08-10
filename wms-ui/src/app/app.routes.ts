import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { authGuard } from './core/guards/auth.guard';
import { moduleGuard } from './core/guards/module.guard';
import { portalGuard } from './core/guards/portal.guard';
import { agentPortalGuard } from './core/guards/agent-portal.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { featureGuard } from './core/guards/feature.guard';
import { mustChangePasswordGuard } from './core/guards/must-change-password.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./modules/auth/auth.routes')
  },
  {
    path: '',
    component: ShellComponent,
    // `mustChangePasswordGuard` shell'ning HAMMA bolasiga qo'llanadi va faqat profil
    // sahifasini o'tkazadi — parol o'sha yerda o'zgartiriladi.
    canActivate: [authGuard],
    canActivateChild: [mustChangePasswordGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./modules/dashboard/dashboard.component')
      },
      // Har marshrutda modul VA ruxsat tekshiriladi. Ilgari ba'zilarida (production,
      // warehouse, transfers, counterparties, products) faqat modul tekshirilardi:
      // backend baribir 403 qaytarardi, lekin foydalanuvchi to'g'ridan-to'g'ri URL bilan
      // kirsa bo'sh sahifa va xato toastiga tushardi — Moliya/KPI esa to'g'ri qaytarardi.
      {
        path: 'warehouse',
        canActivate: [moduleGuard('WAREHOUSE_RAW'), permissionGuard('warehouse.view')],
        loadChildren: () => import('./modules/warehouse/warehouse.routes')
      },
      {
        path: 'production',
        canActivate: [moduleGuard('PRODUCTION'), permissionGuard('production.view')],
        loadChildren: () => import('./modules/production/production.routes')
      },
      {
        path: 'transfers',
        canActivate: [moduleGuard('TRANSFERS'), permissionGuard('transfers.view')],
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
        canActivate: [permissionGuard('partners.view')],
        loadChildren: () => import('./modules/counterparties/counterparties.routes')
      },
      {
        path: 'agents',
        canActivate: [moduleGuard('AGENTS'), permissionGuard('agents.view')],
        loadChildren: () => import('./modules/agents/agents.routes')
      },
      {
        path: 'products',
        canActivate: [permissionGuard('products.view')],
        loadChildren: () => import('./modules/products/products.routes')
      },
      {
        path: 'delivery',
        canActivate: [moduleGuard('DELIVERY'), permissionGuard('delivery.view')],
        loadChildren: () => import('./modules/delivery/delivery.routes')
      },
      {
        path: 'settings',
        loadChildren: () => import('./modules/settings/settings.routes')
      },
      // Maxsus (custom) fitchalar — har biri o'z feature'i bilan yopiladi.
      // Konvensiya: modules/custom/README.md
      {
        path: 'custom/example-feature',
        canActivate: [featureGuard('custom.example-feature')],
        loadComponent: () => import('./modules/custom/example-feature/example-feature.component')
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
  },
  // Noma'lum manzil hech qanday marshrutga tushmasa router outlet'ni bo'sh qoldirardi va
  // foydalanuvchi bo'm-bo'sh oq sahifani ko'rardi. Kirgan bo'lsa — bosh sahifaga,
  // aks holda `authGuard` uni login sahifasiga olib chiqadi.
  { path: '**', redirectTo: 'dashboard' }
];
