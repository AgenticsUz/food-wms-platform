import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { WmsSession } from '../../../core/auth/wms-session';
import { toLocalDateString } from '../../../core/utils/date.util';

const DISMISS_KEY = 'wms.subscriptionBannerDismissed';

/** Muddat tugashiga necha kun qolganda ogohlantiramiz (backend `Subscription:WarnBeforeDays`). */
const WARN_BEFORE_DAYS = 7;

/**
 * Obuna muddati tugashiga oz qolganda qobiq tepasidagi chiziq (eski `subscription-banner`).
 *
 * Manba endi `/api/me` ning `subscription` bo'lagi — alohida `subscription/me`
 * so'rovi qobiqda YO'Q. Kunlar serverda hisoblanadi (`trialDaysLeft`,
 * `paidDaysLeft`); to'lov muddati bo'lsa u trialdan ustun.
 *
 * Blok holati bu yerda chizilmaydi: to'xtatilgan tenant qobiqqa umuman
 * kirmaydi (`tenantGuard` → `/subscription` ekrani).
 *
 * Ogohlantirish kuniga bir marta yopiladi.
 */
@Component({
  selector: 'app-subscription-banner',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './subscription-banner.component.html',
  styleUrl: './subscription-banner.component.scss',
})
export class SubscriptionBannerComponent {
  private readonly session = inject(WmsSession);
  private readonly dismissedOn = signal<string | null>(readDismissed());

  readonly daysLeft = computed(() => {
    const subscription = this.session.subscription();
    if (subscription === null) return null;
    return subscription.paidUntil ? subscription.paidDaysLeft : subscription.trialDaysLeft;
  });

  readonly showWarning = computed(() => {
    const subscription = this.session.subscription();
    const days = this.daysLeft();
    if (subscription === null || !subscription.allowed || days === null) return false;
    if (days < 0 || days > WARN_BEFORE_DAYS) return false;
    return this.dismissedOn() !== toLocalDateString(new Date());
  });

  /** To'lov muddati bo'lsa u haqda, aks holda sinov muddati haqida. */
  readonly warningKey = computed(() =>
    this.session.subscription()?.paidUntil ? 'subscription.paymentExpiringSoon' : 'subscription.expiringSoon'
  );

  dismiss(): void {
    const day = toLocalDateString(new Date());
    try {
      globalThis.localStorage?.setItem(DISMISS_KEY, day);
    } catch {
      // Maxfiylik rejimi — faqat shu sahifa uchun yopiladi.
    }
    this.dismissedOn.set(day);
  }
}

function readDismissed(): string | null {
  try {
    return globalThis.localStorage?.getItem(DISMISS_KEY) ?? null;
  } catch {
    return null;
  }
}
