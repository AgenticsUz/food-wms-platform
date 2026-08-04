import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { SubscriptionService } from '../../../core/services/subscription.service';

const DISMISS_KEY = 'subscriptionBannerDismissed';

/**
 * Muddat tugashiga oz qolganda yoki obuna bloklanganda ko'rsatiladigan chiziq.
 * Ogohlantirish kuniga bir marta yopiladi; blok holatida yopilmaydi —
 * mijoz sababni ko'rishi shart.
 */
@Component({
  selector: 'app-subscription-banner',
  standalone: true,
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './subscription-banner.component.html',
  styleUrl: './subscription-banner.component.scss'
})
export class SubscriptionBannerComponent {
  private service = inject(SubscriptionService);

  info = this.service.info;
  daysLeft = this.service.daysLeft;
  private dismissedOn = signal<string | null>(localStorage.getItem(DISMISS_KEY));

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  blocked = computed(() => this.info()?.isBlocked ?? false);

  showWarning = computed(() => {
    const i = this.info();
    if (!i || i.isBlocked || !this.service.isExpiringSoon()) return false;
    return this.dismissedOn() !== this.today();
  });

  /** To'lov muddati bo'lsa u haqda, aks holda demo muddati haqida yozamiz. */
  warningKey = computed(() =>
    this.info()?.paidUntil ? 'subscription.paymentExpiringSoon' : 'subscription.expiringSoon'
  );

  blockedText = computed(() => this.info()?.blockedMessage ?? null);

  blockedKey = computed(() => {
    switch (this.info()?.blockedReason) {
      case 'payment_expired': return 'subscription.blockedPaymentExpired';
      case 'trial_expired': return 'subscription.blockedTrialExpired';
      case 'tenant_inactive': return 'subscription.blockedInactive';
      default: return 'subscription.blockedSuspended';
    }
  });

  dismiss() {
    const day = this.today();
    localStorage.setItem(DISMISS_KEY, day);
    this.dismissedOn.set(day);
  }
}
