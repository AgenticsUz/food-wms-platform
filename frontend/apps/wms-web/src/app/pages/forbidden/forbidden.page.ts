import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

/**
 * 403 — ruxsat yo'q (`permissionGuard`). Qobiq ICHIDA: menyu joyida qoladi va
 * foydalanuvchi boshqa bo'limga o'ta oladi. Eski ilova bu holatda jimgina
 * `/dashboard` ga qaytarardi — foydalanuvchi nima bo'lganini bilmasdi.
 */
@Component({
  selector: 'app-forbidden-page',
  imports: [TranslocoDirective, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="state-scene state-scene--inline" *transloco="let t">
      <section class="state-card">
        <div class="state-mark state-mark--danger"><i class="pi pi-lock"></i></div>
        <h1 class="state-title">{{ t('auth.forbidden.title') }}</h1>
        <p class="state-subtitle">{{ t('auth.forbidden.subtitle') }}</p>
        <a class="state-link" routerLink="/">{{ t('shell.back') }}</a>
      </section>
    </div>
  `,
  styleUrl: '../state-card.scss',
})
export class ForbiddenPage {}
