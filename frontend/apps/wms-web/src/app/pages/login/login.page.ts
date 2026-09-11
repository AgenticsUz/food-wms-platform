import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthService } from '@agentics/auth';
import { LanguageService, SUPPORTED_LANGUAGES, type AppLanguage } from '@agentics/i18n';
import { ThemeService } from '../../core/theme/theme.service';

const LANGUAGE_SHORT: Readonly<Record<AppLanguage, string>> = {
  'uz-Latn': 'UZ',
  'uz-Cyrl': 'ЎЗ',
  ru: 'RU',
};

/** Ogohlantirish ohangidagi chiqish sabablari (`manual` — neytral). */
const WARNING_REASONS: readonly string[] = ['session-expired', 'idle-timeout', 'unauthorized'];

/** Chap panelda ko'rsatiladigan tizim modullari (menyu `WMS_NAV` bilan bir xil tartib). */
interface LoginModule {
  readonly key: string;
  readonly desc: string;
  readonly icon: string;
}

const LOGIN_MODULES: readonly LoginModule[] = [
  { key: 'nav.warehouse', desc: 'shell.login.modules.warehouse', icon: 'pi pi-box' },
  { key: 'nav.production', desc: 'shell.login.modules.production', icon: 'pi pi-cog' },
  { key: 'nav.transfers', desc: 'shell.login.modules.transfers', icon: 'pi pi-arrow-right-arrow-left' },
  { key: 'nav.finance', desc: 'shell.login.modules.finance', icon: 'pi pi-wallet' },
  { key: 'nav.kpi', desc: 'shell.login.modules.kpi', icon: 'pi pi-chart-line' },
  { key: 'nav.partners', desc: 'shell.login.modules.partners', icon: 'pi pi-users' },
];

const LOGIN_HIGHLIGHTS: readonly { readonly key: string; readonly icon: string }[] = [
  { key: 'shell.login.highlights.realtime', icon: 'pi pi-bolt' },
  { key: 'shell.login.highlights.multiWarehouse', icon: 'pi pi-sitemap' },
  { key: 'shell.login.highlights.security', icon: 'pi pi-shield' },
  { key: 'shell.login.highlights.telegram', icon: 'pi pi-send' },
];

/**
 * Kirish sahifasi — LOGIN FORMASI YO'Q (D5: parol Identity'niki).
 *
 * «Kirish» tugmasi OIDC authorization code + PKCE oqimini boshlaydi
 * (`AuthService.login`), parolni foydalanuvchi `id.agentics.uz` ning o'zida
 * kiritadi. Registratsiya ham, parol almashtirish ham, `mustChangePassword`
 * ham bu ilovada yo'q — hammasi Identity'da.
 *
 * Sahifa ikki qismdan iborat: chapda tizim haqida ma'lumot (modullar,
 * afzalliklar — faqat statik i18n matn, backend so'rovi YO'Q, chunki
 * foydalanuvchi hali autentifikatsiyadan o'tmagan), o'ngda kirish kartochkasi.
 *
 * ⚠️ Nega sahifa umuman bor (darhol yo'naltirilmaydi) — Wash `login.page.ts`
 * izohidagi to'rt sabab: chiqishdan keyin qayta kirish sikliga tushmaslik,
 * callback xatosida issiq sikl bo'lmasligi, PKCE holati yo'qolganda sikl va
 * «sessiya muddati tugadi» sababini ko'rsatadigan yagona joy.
 */
@Component({
  selector: 'app-login-page',
  imports: [TranslocoDirective, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly language = inject(LanguageService);
  private readonly theme = inject(ThemeService);

  /** `authGuard` qo'shadigan qaytish manzili (`withComponentInputBinding`). */
  readonly returnUrl = input<string>('/');
  /** Sessiya tugash sababi: `session-expired`, `idle-timeout`, `unauthorized`, `manual`. */
  readonly reason = input<string | undefined>(undefined);

  readonly languages = SUPPORTED_LANGUAGES;
  readonly shortLabel = LANGUAGE_SHORT;
  readonly activeLanguage = this.language.language;
  readonly isDark = this.theme.isDark;

  readonly modules = LOGIN_MODULES;
  readonly highlights = LOGIN_HIGHLIGHTS;
  readonly year = new Date().getFullYear();

  readonly busy = signal(false);
  /** Kirishni boshlab bo'lmadi (i18n kaliti — paket xatosi shu shaklda). */
  readonly error = signal<string | undefined>(undefined);

  /**
   * Sabab IKKI manbadan: manzil qatori (ilova ichidagi chiqish) yoki
   * `lastLogoutReason` (Identity `endsession` dan qaytgach — manzil qatorida
   * hech narsa qolmaydi, sabab `sessionStorage` orqali o'tadi).
   */
  private readonly activeReason = computed(
    () => this.reason() ?? this.auth.lastLogoutReason() ?? undefined
  );

  readonly reasonKey = computed(() => {
    const value = this.activeReason();
    return value ? `auth.logoutReason.${value}` : undefined;
  });

  readonly isWarning = computed(() => WARNING_REASONS.includes(this.activeReason() ?? ''));

  onLanguage(language: AppLanguage): void {
    this.language.setLanguage(language);
  }

  toggleTheme(): void {
    this.theme.toggle();
  }

  async signIn(): Promise<void> {
    this.error.set(undefined);
    this.busy.set(true);
    try {
      await this.auth.login(this.returnUrl());
    } catch (cause) {
      this.error.set(cause instanceof Error ? cause.message : 'errors.unknown');
      this.busy.set(false);
    }
  }
}
