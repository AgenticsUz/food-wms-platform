import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { IdleTimeoutService } from '@agentics/auth';

import { NotificationBellService } from '../../core/services/notification-bell.service';
import { SubscriptionBannerComponent } from '../../shared/components/subscription-banner/subscription-banner.component';
import { HeaderComponent } from '../header/header.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

/**
 * Himoyalangan zonaning qobig'i (eski `shell.component`): sidebar + topbar +
 * obuna ogohlantirishi + kontent.
 *
 * ⚠️ Harakatsizlik taymeri va bildirishnoma polling'i SHU YERDA boshlanadi, ildizda
 * emas: qobiq — `authGuard` + `tenantGuard` ortidagi yagona kirish nuqtasi. Kirish
 * sahifasida taymer kerak emas, polling esa tokensiz har daqiqa 401 olardi.
 */
@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, SidebarComponent, HeaderComponent, SubscriptionBannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayout {
  readonly sidebarCollapsed = signal(false);
  readonly mobileMenuOpen = signal(false);

  constructor() {
    inject(IdleTimeoutService).start();

    const bell = inject(NotificationBellService);
    bell.startPolling();
    inject(DestroyRef).onDestroy(() => bell.stopPolling());
  }

  onSidebarToggle(collapsed: boolean): void {
    this.sidebarCollapsed.set(collapsed);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }
}
