import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { SubscriptionService } from '../../../core/services/subscription.service';

const DISMISS_KEY = 'subscriptionBannerDismissed';

/**
 * Trial tugashiga oz qolganda (backend `isExpiringSoon`) yoki obuna bloklanganda
 * ko'rsatiladigan chiziq. Ogohlantirish kuniga bir marta yopiladi; blok holatida
 * yopib bo'lmaydi — mijoz sababni ko'rishi shart.
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
  private dismissedOn = signal<string | null>(localStorage.getItem(DISMISS_KEY));

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  blocked = computed(() => this.info()?.isBlocked ?? false);

  showWarning = computed(() => {
    const i = this.info();
    if (!i || i.isBlocked || !i.isExpiringSoon) return false;
    return this.dismissedOn() !== this.today();
  });

  daysLeft = computed(() => this.info()?.trialDaysLeft ?? 0);

  blockedKey = computed(() => {
    switch (this.info()?.blockedReason) {
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
