import { Component, inject, signal, computed, ChangeDetectionStrategy, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  moduleCode?: string;
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

  collapsed = signal(false);
  collapseToggled = output<boolean>();

  currentUser = this.authService.currentUser;

  allNavItems: NavItem[] = [
    { label: 'Dashboard', icon: 'pi pi-th-large', route: '/dashboard' },
    { label: 'Warehouse', icon: 'pi pi-box', route: '/warehouse', moduleCode: 'WAREHOUSE_RAW' },
    { label: 'Production', icon: 'pi pi-cog', route: '/production', moduleCode: 'PRODUCTION' },
    { label: 'Transfers', icon: 'pi pi-arrow-right-arrow-left', route: '/transfers', moduleCode: 'TRANSFERS' },
    { label: 'Finance', icon: 'pi pi-wallet', route: '/finance', moduleCode: 'FINANCE' },
    { label: 'KPI', icon: 'pi pi-chart-line', route: '/kpi', moduleCode: 'KPI' },
    { label: 'Partners', icon: 'pi pi-users', route: '/counterparties' },
    { label: 'Products', icon: 'pi pi-tags', route: '/products' },
  ];

  visibleNavItems = computed(() => {
    return this.allNavItems.filter(item => {
      if (!item.moduleCode) return true;
      return this.tenantService.isModuleEnabled(item.moduleCode);
    });
  });

  toggleCollapse() {
    this.collapsed.update(v => !v);
    this.collapseToggled.emit(this.collapsed());
  }

  logout() {
    this.authService.logout();
  }
}
