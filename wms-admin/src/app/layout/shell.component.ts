import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../core/services/auth.service';
import { NotificationService } from '../core/services/notification.service';

const LANG_KEY = 'adminLang';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private auth = inject(AuthService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  user = this.auth.currentUser;

  nav = [
    { key: 'nav.dashboard', icon: 'pi pi-chart-bar', route: '/dashboard' },
    { key: 'nav.tenants', icon: 'pi pi-building', route: '/tenants' },
    { key: 'nav.leads', icon: 'pi pi-inbox', route: '/leads' },
    { key: 'nav.organizations', icon: 'pi pi-sitemap', route: '/organizations' },
    { key: 'nav.plans', icon: 'pi pi-tags', route: '/plans' }
  ];

  readonly langs = [
    { code: 'uz', label: "O'z" },
    { code: 'ru', label: 'Ру' },
    { code: 'en', label: 'En' }
  ];

  activeLang = signal(this.transloco.getActiveLang());

  switchLang(code: string) {
    this.transloco.setActiveLang(code);
    localStorage.setItem(LANG_KEY, code);
    this.activeLang.set(code);
  }

  logout() {
    this.notify.confirmAction(
      this.transloco.translate('nav.signOutConfirm'),
      this.transloco.translate('nav.signOutHeader'),
      () => this.auth.logout()
    );
  }
}
