import { ChangeDetectionStrategy, Component, inject, input, signal, type OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { AUTH_ROUTES, AuthService } from '@agentics/auth';

/**
 * OIDC qaytish nuqtasi (`redirect_uri` = `/auth/callback`, Identity'da ro'yxatda).
 *
 * Identity `?code=&state=` bilan qaytaradi; `AuthService.handleCallback` kodni
 * BFF sessiya endpointida tokenga almashtiradi (refresh token httpOnly cookie'da
 * qoladi), so'ng `GET /api/me` ni yuklaydi. Xato (CSRF `state`, eskirgan kod,
 * `/me` rad etdi) → login sahifasi `?reason=unauthorized` — bo'sh ekran QOLMAYDI.
 */
@Component({
  selector: 'app-auth-callback-page',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="state-scene" *transloco="let t">
      <section class="state-card">
        <div class="state-mark">
          <i class="pi" [class.pi-spin]="!failed()" [class.pi-spinner]="!failed()" [class.pi-times]="failed()"></i>
        </div>
        <p class="state-subtitle">
          {{ failed() ? t('auth.callback.failed') : t('auth.callback.pending') }}
        </p>
      </section>
    </main>
  `,
  styleUrl: '../state-card.scss',
})
export class AuthCallbackPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly routes = inject(AUTH_ROUTES);

  readonly code = input<string | undefined>(undefined);
  readonly state = input<string | undefined>(undefined);
  readonly error = input<string | undefined>(undefined);

  protected readonly failed = signal(false);

  /**
   * ⚠️ `ngOnInit`, konstruktor EMAS: marshrutdan kelgan `input()` qiymatlari
   * komponent yaratilgandan KEYIN o'rnatiladi — konstruktorda `code()` doim
   * `undefined` bo'lardi.
   */
  ngOnInit(): void {
    void this.exchange();
  }

  private async exchange(): Promise<void> {
    const code = this.code();
    const state = this.state();
    const returnUrl =
      this.error() === undefined && code !== undefined && state !== undefined
        ? await this.auth.handleCallback(code, state)
        : null;

    if (returnUrl === null) {
      this.failed.set(true);
      await this.router.navigate([this.routes.login], { queryParams: { reason: 'unauthorized' } });
      return;
    }
    await this.router.navigateByUrl(returnUrl);
  }
}
