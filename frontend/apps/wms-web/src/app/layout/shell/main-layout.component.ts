import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { IdleTimeoutService } from '@agentics/auth';

import { WmsSession } from '../../core/auth/wms-session';
import { AiDrawerComponent } from '../../features/ai/ai-drawer.component';
import { AiStore } from '../../features/ai/ai.store';
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
  imports: [RouterOutlet, SidebarComponent, HeaderComponent, SubscriptionBannerComponent, AiDrawerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayout {
  private readonly session = inject(WmsSession);

  readonly ai = inject(AiStore);

  /**
   * AI tugmasi `ai.chat` feature'i yoqilganda KO'RINADI.
   *
   * ⚠️ Bu faqat UX: haqiqiy chegara backendda — gateway feature'ni o'zi tekshiradi
   * va `ai_disabled` qaytaradi. Lekin yoqilmagan tenantga tugma ko'rsatish
   * «bosdim — ishlamadi» degan taassurot qoldirardi.
   */
  readonly aiEnabled = computed(() => this.session.isFeatureEnabled('ai.chat'));

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
