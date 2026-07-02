import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ProgressBar } from 'primeng/progressbar';
import { ThemeService } from './core/services/theme.service';
import { TranslocoService } from '@jsverse/transloco';
import { LoadingService } from './core/services/loading.service';
import { AuthService } from './core/services/auth.service';
import { NotificationBellService } from './core/services/notification-bell.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, ConfirmDialog, ProgressBar],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App implements OnInit {
  private themeService = inject(ThemeService);
  private transloco = inject(TranslocoService);
  private router = inject(Router);
  loadingService = inject(LoadingService);
  private authService = inject(AuthService);
  private bellService = inject(NotificationBellService);

  ngOnInit() {
    this.themeService.init();
    const savedLang = localStorage.getItem('lang');
    if (savedLang && ['uz', 'uz-cyrl', 'ru', 'en'].includes(savedLang)) {
      this.transloco.setActiveLang(savedLang);
    }

    // Faqat asosiy ilova sessiyasida (portal/agent-portal route'larida emas) polling
    // boshlaymiz — aks holda portal foydalanuvchisidagi eski asosiy token 401 → logout keltiradi.
    // Permissionlarni authGuard yangilaydi (bu yerda takrorlamaymiz).
    const path = window.location.pathname;
    const isPortalRoute = path.startsWith('/portal') || path.startsWith('/agent-portal');
    if (this.authService.isAuthenticated() && !isPortalRoute) {
      this.bellService.startPolling();
    }

    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.loadingService.show();
      }
      if (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) {
        this.loadingService.hide();
      }
    });
  }
}
