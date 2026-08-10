import { Component, computed, inject, input, ChangeDetectionStrategy } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { SubscriptionService } from '../../../core/services/subscription.service';

/**
 * Yaratish formalari ustidagi tinch panel: "Plan limitiga yaqinlashdingiz: 20 / 25".
 *
 * **Bloklamaydi** — limitga urilganda backend 402 beradi, bu esa undan oldingi
 * ogohlantirish. Foiz ham, chegara ham backenddan keladi (`isNearLimit`), bu yerda
 * hech narsa hisoblanmaydi.
 *
 * Plansiz tenantda `isNearLimit` — `null`, ya'ni panel umuman chiqmaydi.
 */
@Component({
  selector: 'app-limit-notice',
  standalone: true,
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (show()) {
      <div class="limit-notice" *transloco="let t">
        <i class="pi pi-info-circle"></i>
        <span>{{ t('subscription.nearLimit', { used: used(), max: max() }) }}</span>
      </div>
    }
  `,
  styles: [`
    .limit-notice {
      display: flex; align-items: center; gap: 8px;
      margin-bottom: 12px; padding: 9px 12px;
      border-radius: var(--radius-sm); font-size: 12px;
      background: var(--color-caramel-50, var(--bg-inset));
      color: var(--color-caramel-700, var(--text-secondary));
      border-left: 3px solid var(--color-caramel-500, var(--color-blueberry-500));
      i { font-size: 13px; flex-shrink: 0; }
    }
  `]
})
export class LimitNoticeComponent {
  /** Qaysi limit kuzatilyapti. */
  kind = input.required<'users' | 'warehouses' | 'transfers'>();

  private subscription = inject(SubscriptionService);

  private detail = computed(() => this.subscription.info()?.limits?.[this.kind()] ?? null);

  show = computed(() => this.detail()?.isNearLimit === true);
  used = computed(() => this.detail()?.current ?? 0);
  max = computed(() => this.detail()?.max ?? 0);
}
