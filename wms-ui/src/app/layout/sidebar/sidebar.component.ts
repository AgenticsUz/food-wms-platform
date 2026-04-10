import { Component, inject, signal, computed, ChangeDetectionStrategy, output } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';

export interface NavItem {
  label: string;
  icon: string;
  route?: string;
  moduleCode?: string;
  children?: { label: string; route: string }[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private tenantService = inject(TenantService);
  private authService = inject(AuthService);
  private router = inject(Router);

  collapsed = signal(false);
  collapseToggled = output<boolean>();
  expandedMenu = signal<string | null>(null);

  currentUser = this.authService.currentUser;

  allNavItems: NavItem[] = [
    { label: 'Dashboard', icon: 'pi pi-th-large', route: '/dashboard' },
    {
      label: 'Warehouse', icon: 'pi pi-box', moduleCode: 'WAREHOUSE_RAW',
      children: [
        { label: 'Stock Overview', route: '/warehouse' },
        { label: 'Warehouses', route: '/warehouse/warehouses' },
        { label: 'Locations', route: '/warehouse/locations' },
        { label: 'Batches', route: '/warehouse/batches' },
        { label: 'Movements', route: '/warehouse/movements' }
      ]
    },
    {
      label: 'Production', icon: 'pi pi-cog', moduleCode: 'PRODUCTION',
      children: [
        { label: 'Orders', route: '/production/orders' },
        { label: 'Recipes', route: '/production/recipes' },
        { label: 'Stages', route: '/production/stages' }
      ]
    },
    {
      label: 'Transfers', icon: 'pi pi-arrow-right-arrow-left', moduleCode: 'TRANSFERS',
      route: '/transfers'
    },
    {
      label: 'Finance', icon: 'pi pi-wallet', moduleCode: 'FINANCE',
      children: [
        { label: 'Overview', route: '/finance' },
        { label: 'Transactions', route: '/finance/transactions' },
        { label: 'Debts', route: '/finance/debts' },
        { label: 'Payments', route: '/finance/payments' }
      ]
    },
    {
      label: 'KPI', icon: 'pi pi-chart-line', moduleCode: 'KPI',
      children: [
        { label: 'Dashboard', route: '/kpi' },
        { label: 'Shifts', route: '/kpi/shifts' },
        { label: 'Plans', route: '/kpi/plans' },
        { label: 'Actuals', route: '/kpi/actuals' },
        { label: 'Attendance', route: '/kpi/attendance' }
      ]
    },
    {
      label: 'Partners', icon: 'pi pi-users',
      children: [
        { label: 'Suppliers', route: '/counterparties/suppliers' },
        { label: 'Clients', route: '/counterparties/clients' }
      ]
    },
    {
      label: 'Products', icon: 'pi pi-tags',
      children: [
        { label: 'Products', route: '/products' },
        { label: 'Categories', route: '/products/categories' },
        { label: 'Units', route: '/products/units' }
      ]
    },
  ];

  settingsItem: NavItem = {
    label: 'Settings', icon: 'pi pi-sliders-h',
    children: [
      { label: 'Users', route: '/settings/users' },
      { label: 'Roles', route: '/settings/roles' },
      { label: 'Modules', route: '/settings/modules' },
      { label: 'QC Parameters', route: '/settings/qc-parameters' },
      { label: 'My Profile', route: '/settings/profile' }
    ]
  };

  visibleNavItems = computed(() => {
    return this.allNavItems.filter(item => {
      if (!item.moduleCode) return true;
      return this.tenantService.isModuleEnabled(item.moduleCode);
    });
  });

  toggleCollapse() {
    this.collapsed.update(v => !v);
    if (this.collapsed()) this.expandedMenu.set(null);
    this.collapseToggled.emit(this.collapsed());
  }

  toggleMenu(label: string) {
    if (this.collapsed()) {
      this.collapsed.set(false);
      this.collapseToggled.emit(false);
      this.expandedMenu.set(label);
      return;
    }
    this.expandedMenu.update(v => v === label ? null : label);
  }

  isExpanded(label: string): boolean {
    return this.expandedMenu() === label;
  }

  isMenuActive(item: NavItem): boolean {
    const url = this.router.url;
    if (item.route) return url === item.route || url.startsWith(item.route + '/');
    if (item.children) return item.children.some(c => url === c.route || url.startsWith(c.route + '/'));
    return false;
  }

  logout() {
    this.authService.logout();
  }
}
