import { ChangeDetectionStrategy, Component, computed, inject, output, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Menu } from 'primeng/menu';
import type { MenuItem } from 'primeng/api';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthService } from '@agentics/auth';
import { LanguageService, SUPPORTED_LANGUAGES, type AppLanguage } from '@agentics/i18n';

import { WmsSession } from '../../core/auth/wms-session';
import { NotificationService } from '../../core/notify/notification.service';
import { CurrencyService } from '../../core/services/currency.service';
import { ThemeService } from '../../core/theme/theme.service';
import { NotificationBellComponent } from '../../shared/components/notification-bell/notification-bell.component';

/**
 * Tilning O'Z yozuvidagi qisqa kodi — tarjima qilinmaydi («RU» rus tilida ham «RU»).
 * To'liq nom `title` da (`common.language.*`).
 */
const LANGUAGE_SHORT: Readonly<Record<AppLanguage, string>> = {
  'uz-Latn': 'UZ',
  'uz-Cyrl': 'ЎЗ',
  ru: 'RU',
};

/**
 * Topbar (eski `header.component`): tenant nomi, valyuta kurslari, til
 * (endi UCH til — D11), mavzu, bildirishnomalar, foydalanuvchi menyusi.
 *
 * Til `LanguageService` orqali: u tanlovni `wms.language` da saqlaydi va
 * `Accept-Language` ni (`localeInterceptor`) shunga ulaydi — server xabarlari
 * ham tanlangan tilda keladi.
 */
@Component({
  selector: 'app-header',
  imports: [DecimalPipe, Menu, TranslocoDirective, NotificationBellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnInit {
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  readonly theme = inject(ThemeService);
  readonly currency = inject(CurrencyService);

  readonly menuToggled = output<void>();

  readonly languages = SUPPORTED_LANGUAGES;
  readonly shortLabel = LANGUAGE_SHORT;
  readonly activeLanguage = this.language.language;

  readonly fullName = this.session.fullName;
  readonly tenantName = computed(() => this.session.tenant()?.name ?? '');

  readonly mainRates = computed(() => {
    const rates = this.currency.rates();
    return ['USD', 'EUR', 'RUB'].map((code) => ({ code, rate: rates[code] ?? 0 })).filter((r) => r.rate > 0);
  });

  /**
   * Menyu bandlari OCHILGANDA yig'iladi: matn tarjima qilingan satr bo'lib
   * `p-menu` ga beriladi, ya'ni `computed` bo'lsa u til chunk'i yuklanmasdan
   * oldin hisoblanib, kalitning o'zi bilan qotib qolishi mumkin edi.
   */
  readonly userMenuItems = signal<MenuItem[]>([]);

  ngOnInit(): void {
    this.currency.loadRates();
  }

  onLanguage(language: AppLanguage): void {
    this.language.setLanguage(language);
  }

  toggleUserMenu(event: Event, menu: Menu): void {
    const t = (key: string): string => this.language.translate(key);
    this.userMenuItems.set([
      { label: t('shell.profile'), icon: 'pi pi-user', routerLink: '/settings/profile' },
      { separator: true },
      { label: t('auth.logout'), icon: 'pi pi-sign-out', command: () => this.logout() },
    ]);
    menu.toggle(event);
  }

  private logout(): void {
    this.notify.confirmAction(
      this.language.translate('auth.confirmLogout'),
      this.language.translate('auth.logoutTitle'),
      () => void this.auth.logout('manual')
    );
  }
}
