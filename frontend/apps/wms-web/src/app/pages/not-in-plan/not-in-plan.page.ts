import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

/**
 * «Tarifingizda yo'q» — `moduleGuard` / `featureGuard` shu yerga yo'naltiradi
 * (`?module=PRODUCTION` yoki `?feature=warehouse.batches`). Qobiq ICHIDA.
 *
 * Bu ruxsat muammosi EMAS (u `/forbidden`): odamda huquq bor, lekin
 * tashkilotning tarifida bu imkoniyat yo'q. Ikkisini aralashtirish
 * foydalanuvchini noto'g'ri odamga (o'z administratoriga emas, Agentics'ga
 * yoki aksincha) yuborardi.
 */
@Component({
  selector: 'app-not-in-plan-page',
  imports: [TranslocoDirective, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="state-scene state-scene--inline" *transloco="let t">
      <section class="state-card">
        <div class="state-mark state-mark--warn"><i class="pi pi-lock"></i></div>
        <h1 class="state-title">{{ t('shell.notInPlan.title') }}</h1>
        <p class="state-subtitle">
          @if (module(); as code) {
            {{ t('shell.notInPlan.module', { name: t('shell.modules.' + code) }) }}
          } @else {
            {{ t('shell.notInPlan.feature') }}
          }
        </p>
        <p class="state-meta">{{ t('shell.notInPlan.hint') }}</p>
        <a class="state-link" routerLink="/">{{ t('shell.back') }}</a>
      </section>
    </div>
  `,
  styleUrl: '../state-card.scss',
})
export class NotInPlanPage {
  /** Modul kodi (`?module=`) — `shell.modules.<CODE>` nomi bilan ko'rsatiladi. */
  readonly module = input<string | undefined>(undefined);
  /** Feature kodi (`?feature=`) — foydalanuvchiga kod ko'rsatilmaydi. */
  readonly feature = input<string | undefined>(undefined);
}
