import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs/operators';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { NotificationService } from '../core/services/notification.service';

const LANG_KEY = 'adminLang';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoDirective, Menu],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private auth = inject(AuthService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);
  private router = inject(Router);
  theme = inject(ThemeService);

  user = this.auth.currentUser;

  nav = [
    { key: 'nav.dashboard', icon: 'pi pi-chart-bar', route: '/dashboard' },
    { key: 'nav.tenants', icon: 'pi pi-building', route: '/tenants' },
    { key: 'nav.leads', icon: 'pi pi-inbox', route: '/leads' },
    { key: 'nav.organizations', icon: 'pi pi-sitemap', route: '/organizations' },
    { key: 'nav.plans', icon: 'pi pi-tags', route: '/plans' }
  ];

  /** Sarlavha marshrutdan olinadi — har sahifa o'z h1 ini takrorlamasin. */
  private url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );

  pageTitleKey = computed(() => {
    const current = this.url().split('?')[0];
    return this.nav.find(n => current.startsWith(n.route))?.key ?? 'nav.dashboard';
  });

  readonly langs = [
    { code: 'uz', label: "O'z" },
    { code: 'ru', label: 'Ру' },
    { code: 'en', label: 'En' }
  ];
  activeLang = signal(this.transloco.getActiveLang());

  mobileNavOpen = signal(false);

  userMenu = computed<MenuItem[]>(() => [
    {
      label: this.transloco.translate('nav.signOut'),
      icon: 'pi pi-sign-out',
      command: () => this.logout()
    }
  ]);

  switchLang(code: string) {
    this.transloco.setActiveLang(code);
    localStorage.setItem(LANG_KEY, code);
    this.activeLang.set(code);
  }

  toggleMobileNav() { this.mobileNavOpen.update(v => !v); }
  closeMobileNav() { this.mobileNavOpen.set(false); }

  userInitial = computed(() => (this.user()?.fullName || 'A').charAt(0).toUpperCase());

  logout() {
    this.notify.confirmAction(
      this.transloco.translate('nav.signOutConfirm'),
      this.transloco.translate('nav.signOutHeader'),
      () => this.auth.logout()
    );
  }
}
