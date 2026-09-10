import { ChangeDetectionStrategy, Component, computed, inject, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { filter, map } from 'rxjs';
import { AuthService } from '@agentics/auth';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../core/auth/wms-session';
import { BrandingService } from '../../core/branding/branding.service';
import { NotificationService } from '../../core/notify/notification.service';
import { SETTINGS_NAV, WMS_NAV, type NavChild, type NavItem } from '../nav';

/**
 * Chap menyu (eski `sidebar.component`): mijoz logotipi, bo'limlar, sozlamalar,
 * foydalanuvchi va chiqish.
 *
 * Eski ilovadan farqlar:
 *  - manba `WmsSession` (`/api/me`) — `PermissionService`/`TenantService`/
 *    `FeatureService` localStorage nusxalari o'chdi;
 *  - parol majburiyati (`navLocked`) o'chdi — parol Identity'niki (D5);
 *  - faol bo'lim URL'i SIGNAL (`currentUrl`): zoneless + OnPush'da `router.url`
 *    ni shablonda o'qish navigatsiyadan keyin qayta chizilmasdi.
 */
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {
  private readonly session = inject(WmsSession);
  private readonly branding = inject(BrandingService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);

  readonly collapsed = signal(false);
  readonly collapseToggled = output<boolean>();
  readonly expandedMenu = signal<string | null>(null);

  readonly wideLogo = this.branding.wideLogoSrc;
  readonly squareLogo = this.branding.squareLogoSrc;
  readonly tenantName = computed(() => this.session.tenant()?.name ?? this.branding.tenantName() ?? '');
  readonly fullName = this.session.fullName;
  readonly role = this.session.primaryRole;

  readonly userInitials = computed(() => {
    const parts = this.fullName().split(' ').filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return this.fullName().substring(0, 2).toUpperCase() || 'U';
  });

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  readonly settingsItem = SETTINGS_NAV;

  readonly visibleNavItems = computed(() => WMS_NAV.filter((item) => this.isVisible(item)));

  visibleChildren(children: readonly NavChild[] | undefined): readonly NavChild[] {
    return (children ?? []).filter(
      (c) =>
        (!c.featureCode || this.session.isFeatureEnabled(c.featureCode)) &&
        (!c.permissionCode || this.session.can(c.permissionCode))
    );
  }

  toggleCollapse(): void {
    this.collapsed.update((v) => !v);
    if (this.collapsed()) this.expandedMenu.set(null);
    this.collapseToggled.emit(this.collapsed());
  }

  toggleMenu(key: string): void {
    if (this.collapsed()) {
      this.collapsed.set(false);
      this.collapseToggled.emit(false);
      this.expandedMenu.set(key);
      return;
    }
    this.expandedMenu.update((v) => (v === key ? null : key));
  }

  isExpanded(key: string): boolean {
    return this.expandedMenu() === key;
  }

  isMenuActive(item: NavItem): boolean {
    const url = this.currentUrl();
    const matches = (route: string): boolean => url === route || url.startsWith(`${route}/`);
    if (item.route) return matches(item.route);
    return (item.children ?? []).some((c) => matches(c.route));
  }

  logout(): void {
    this.notify.confirmAction(
      this.language.translate('auth.confirmLogout'),
      this.language.translate('auth.logoutTitle'),
      () => void this.auth.logout('manual')
    );
  }

  /** Tartib: modul → feature → ruxsat (`nav.ts` izohi). */
  private isVisible(item: NavItem): boolean {
    if (item.moduleCode && !this.session.isModuleEnabled(item.moduleCode)) return false;
    if (item.featureCode && !this.session.isFeatureEnabled(item.featureCode)) return false;
    if (item.permissionCode && !this.session.can(item.permissionCode)) return false;
    // Bolalari bo'lgan guruh — hech bir bolasi ko'rinmasa guruh ham yo'q.
    return !item.children || this.visibleChildren(item.children).length > 0;
  }
}
