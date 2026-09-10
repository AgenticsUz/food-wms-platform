import { ChangeDetectionStrategy, Component, computed, inject, input, type OnInit } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import type { LimitKind } from './subscription.model';
import { SubscriptionService } from './subscription.service';

/**
 * Yaratish formalari ustidagi tinch panel: «Plan limitiga yaqinlashdingiz: 20 / 25»
 * (eski `shared/components/limit-notice`).
 *
 * BLOKLAMAYDI — limitga urilganda backend 402 beradi, bu undan oldingi ogohlantirish.
 * Foiz ham, chegara ham backenddan (`isNearLimit`); plansiz tenantda `null` — panel
 * chiqmaydi.
 *
 * Nega `settings/subscription` da: manba — shu bo'limning `SubscriptionService` i.
 * Ombor/transfer yaratish dialoglari shu yerdan import qiladi.
 *
 * F6 (D7): foydalanuvchi limiti faqat KO'RSATILADI — odamni Console biriktiradi,
 * shuning uchun `kind="users"` yaratish formasi endi yo'q.
 */
@Component({
  selector: 'app-limit-notice',
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
  styles: `
    .limit-notice {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
      padding: 9px 12px;
      border-radius: var(--radius-sm);
      font-size: 12px;
      background: var(--color-caramel-50, var(--bg-inset));
      color: var(--color-caramel-700, var(--text-secondary));
      border-left: 3px solid var(--color-caramel-500, var(--color-blueberry-500));
      i {
        font-size: 13px;
        flex-shrink: 0;
      }
    }
  `,
})
export class LimitNoticeComponent implements OnInit {
  /** Qaysi limit kuzatilyapti. */
  readonly kind = input.required<LimitKind>();

  private readonly subscription = inject(SubscriptionService);

  private readonly detail = computed(() => this.subscription.info()?.limits[this.kind()] ?? null);

  readonly show = computed(() => this.detail()?.isNearLimit === true);
  readonly used = computed(() => this.detail()?.current ?? 0);
  readonly max = computed(() => this.detail()?.max ?? 0);

  ngOnInit(): void {
    // Qobiq `subscription/me` ni o'qimaydi (qisqa holat `/api/me` da) — panel
    // birinchi ochilganda o'zi yuklaydi.
    this.subscription.ensureLoaded();
  }
}
