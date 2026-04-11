import { Component, inject, signal, computed, ChangeDetectionStrategy, output } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TranslocoService } from '@jsverse/transloco';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { PermissionService } from '../../core/services/permission.service';
import { NotificationService } from '../../shared/services/notification.service';

export interface NavChild {
  key: string;
  route: string;
  permissionCode?: string;
}

export interface NavItem {
  key: string;
  icon: string;
  route?: string;
  moduleCode?: string;
  permissionCode?: string;
  children?: NavChild[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private tenantService = inject(TenantService);
  private authService = inject(AuthService);
  private permissionService = inject(PermissionService);
  private router = inject(Router);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  collapsed = signal(false);
  collapseToggled = output<boolean>();
  expandedMenu = signal<string | null>(null);

  currentUser = this.authService.currentUser;

  userInitials = computed(() => {
    const name = this.currentUser()?.fullName ?? '';
    const parts = name.split(' ').filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return name.substring(0, 2).toUpperCase() || 'U';
  });

  allNavItems: NavItem[] = [
    { key: 'nav.dashboard', icon: 'pi pi-th-large', route: '/dashboard' },
    {
      key: 'nav.warehouse', icon: 'pi pi-box', moduleCode: 'WAREHOUSE_RAW', permissionCode: 'warehouse.view',
      children: [
        { key: 'warehouse.stockOverview', route: '/warehouse' },
        { key: 'warehouse.warehouses', route: '/warehouse/warehouses' },
        { key: 'warehouse.locations', route: '/warehouse/locations' },
        { key: 'warehouse.batches', route: '/warehouse/batches' },
        { key: 'warehouse.movements', route: '/warehouse/movements' }
      ]
    },
    {
      key: 'nav.production', icon: 'pi pi-cog', moduleCode: 'PRODUCTION', permissionCode: 'production.view',
      children: [
        { key: 'production.orders', route: '/production/orders' },
        { key: 'production.recipes', route: '/production/recipes' },
        { key: 'production.stages', route: '/production/stages' }
      ]
    },
    {
      key: 'nav.transfers', icon: 'pi pi-arrow-right-arrow-left', moduleCode: 'TRANSFERS', permissionCode: 'transfers.view',
      route: '/transfers'
    },
    {
      key: 'nav.finance', icon: 'pi pi-wallet', moduleCode: 'FINANCE', permissionCode: 'finance.view',
      children: [
        { key: 'finance.overview', route: '/finance' },
        { key: 'finance.transactions', route: '/finance/transactions' },
        { key: 'finance.debts', route: '/finance/debts' },
        { key: 'finance.payments', route: '/finance/payments' }
      ]
    },
    {
      key: 'nav.kpi', icon: 'pi pi-chart-line', moduleCode: 'KPI', permissionCode: 'kpi.view',
      children: [
        { key: 'kpi.dashboard', route: '/kpi' },
        { key: 'kpi.shifts', route: '/kpi/shifts' },
        { key: 'kpi.plans', route: '/kpi/plans' },
        { key: 'kpi.actuals', route: '/kpi/actuals' },
        { key: 'kpi.attendance', route: '/kpi/attendance' }
      ]
    },
    {
      key: 'nav.counterparties', icon: 'pi pi-users', permissionCode: 'partners.view',
      children: [
        { key: 'partners.suppliers', route: '/counterparties/suppliers' },
        { key: 'partners.clients', route: '/counterparties/clients' }
      ]
    },
    {
      key: 'nav.products', icon: 'pi pi-tags', permissionCode: 'products.view',
      children: [
        { key: 'products.products', route: '/products' },
        { key: 'products.categories', route: '/products/categories' },
        { key: 'products.units', route: '/products/units' }
      ]
    },
  ];

  settingsItem: NavItem = {
    key: 'nav.settings', icon: 'pi pi-sliders-h',
    children: [
      { key: 'settings.users', route: '/settings/users', permissionCode: 'settings.users' },
      { key: 'settings.roles', route: '/settings/roles', permissionCode: 'settings.roles' },
      { key: 'settings.modules', route: '/settings/modules', permissionCode: 'settings.modules' },
      { key: 'settings.qcParameters', route: '/settings/qc-parameters' },
      { key: 'settings.profile', route: '/settings/profile' }
    ]
  };

  visibleNavItems = computed(() => {
    return this.allNavItems.filter(item => {
      if (item.moduleCode && !this.tenantService.isModuleEnabled(item.moduleCode)) return false;
      if (item.permissionCode && !this.permissionService.can(item.permissionCode)) return false;
      return true;
    });
  });

  getVisibleChildren(children: NavChild[]): NavChild[] {
    return children.filter(c => !c.permissionCode || this.permissionService.can(c.permissionCode));
  }

  toggleCollapse() {
    this.collapsed.update(v => !v);
    if (this.collapsed()) this.expandedMenu.set(null);
    this.collapseToggled.emit(this.collapsed());
  }

  toggleMenu(key: string) {
    if (this.collapsed()) {
      this.collapsed.set(false);
      this.collapseToggled.emit(false);
      this.expandedMenu.set(key);
      return;
    }
    this.expandedMenu.update(v => v === key ? null : key);
  }

  isExpanded(key: string): boolean {
    return this.expandedMenu() === key;
  }

  isMenuActive(item: NavItem): boolean {
    const url = this.router.url;
    if (item.route) return url === item.route || url.startsWith(item.route + '/');
    if (item.children) return item.children.some(c => url === c.route || url.startsWith(c.route + '/'));
    return false;
  }

  logout() {
    this.notify.confirmAction(
      this.transloco.translate('auth.confirmLogout'),
      this.transloco.translate('auth.logoutTitle'),
      () => this.authService.logout()
    );
  }
}
