import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthService, IdleTimeoutService } from '@agentics/auth';
import { LanguageService, SUPPORTED_LANGUAGES, type AppLanguage } from '@agentics/i18n';
import { Button } from 'primeng/button';

import { ThemeService } from '../../../core/theme/theme.service';
import { BrandMarkComponent } from '../../../shared/components/brand-mark/brand-mark.component';
import { PortalActorKind } from '../portal.model';
import { PortalStore } from '../portal.store';

const LANGUAGE_SHORT: Readonly<Record<AppLanguage, string>> = {
  'uz-Latn': 'UZ',
  'uz-Cyrl': 'ЎЗ',
  ru: 'RU',
};

/**
 * Kabinet qobig'i — ilova qobig'idan ALOHIDA (F9).
 *
 * Nega sidebar'li umumiy qobiq emas: kabinet foydalanuvchisining WMS ruxsati
 * ataylab bo'sh, ya'ni menyuning har bir bandi yashirin bo'lardi va u bo'sh
 * qora chiziqni ko'rardi. Bu yerda uch-to'rtta band, tepada.
 *
 * ⚠️ Harakatsizlik taymeri shu yerda ham boshlanadi (ilova qobig'idagi sabab):
 * kabinet ham `authGuard` ortidagi mustaqil kirish nuqtasi.
 */
@Component({
  selector: 'app-portal-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoDirective, Button, BrandMarkComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-layout.component.html',
  styleUrl: './portal-layout.component.scss',
})
export class PortalLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly language = inject(LanguageService);
  protected readonly store = inject(PortalStore);
  protected readonly theme = inject(ThemeService);

  readonly menuOpen = signal(false);

  readonly isAgent = computed(() => this.store.me()?.kind === PortalActorKind.Agent);
  readonly languages = SUPPORTED_LANGUAGES;
  readonly currentLanguage = this.language.language;

  constructor() {
    inject(IdleTimeoutService).start();
  }

  ngOnInit(): void {
    this.store.ensure().subscribe({ error: () => undefined });
  }

  shortLanguage(code: AppLanguage): string {
    return LANGUAGE_SHORT[code];
  }

  setLanguage(code: AppLanguage): void {
    this.language.setLanguage(code);
  }

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    void this.auth.logout('manual');
  }
}
