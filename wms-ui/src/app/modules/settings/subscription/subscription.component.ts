import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { ProgressBar } from 'primeng/progressbar';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SubscriptionService } from '../../../core/services/subscription.service';
import {
  SubscriptionInfo, SubscriptionPlan, LimitUsage, SubscriptionStatusCode
} from '../../../core/models/subscription.model';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-subscription',
  standalone: true,
  imports: [DatePipe, DecimalPipe, TableModule, ProgressBar, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './subscription.component.html',
  styleUrl: './subscription.component.scss'
})
export default class SubscriptionComponent implements OnInit {
  private service = inject(SubscriptionService);

  info = this.service.info;
  plans = signal<SubscriptionPlan[]>([]);
  loading = signal(true);

  readonly Status = SubscriptionStatusCode;
  readonly supportPhone = environment.supportPhone;
  readonly supportEmail = environment.supportEmail;

  /** Trial tugagan, lekin grace davri hali davom etmoqda. */
  inGrace = computed(() => {
    const i = this.info();
    return !!i && i.trialDaysLeft !== null && i.trialDaysLeft < 0 && !i.isBlocked;
  });

  graceDaysLeft = computed(() => {
    const i = this.info();
    if (!i || i.trialDaysLeft === null) return 0;
    return Math.max(0, i.graceDays + i.trialDaysLeft);
  });

  ngOnInit() {
    this.service.fetch().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });
    this.service.plans().subscribe(res => {
      if (res.success && res.data) this.plans.set(res.data);
    });
  }

  statusKey(status: SubscriptionStatusCode): string {
    switch (status) {
      case SubscriptionStatusCode.Trial: return 'subscription.statusTrial';
      case SubscriptionStatusCode.Active: return 'subscription.statusActive';
      default: return 'subscription.statusSuspended';
    }
  }

  statusClass(status: SubscriptionStatusCode): string {
    switch (status) {
      case SubscriptionStatusCode.Active: return 'pill pill-success';
      case SubscriptionStatusCode.Trial: return 'pill pill-warning';
      default: return 'pill pill-danger';
    }
  }

  blockedKey(info: SubscriptionInfo): string {
    switch (info.blockedReason) {
      case 'subscription_suspended': return 'subscription.blockedSuspended';
      case 'trial_expired': return 'subscription.blockedTrialExpired';
      case 'tenant_inactive': return 'subscription.blockedInactive';
      default: return 'subscription.blockedSuspended';
    }
  }

  limitKey(limit: LimitUsage): string {
    switch (limit.key) {
      case 'users': return 'subscription.limitUsers';
      case 'warehouses': return 'subscription.limitWarehouses';
      default: return 'subscription.limitTransfers';
    }
  }

  /** Progress bar rangi: to'lgan → qizil, ogohlantirish chegarasidan oshgan → sariq. */
  limitClass(limit: LimitUsage, warnPercent: number): string {
    if (limit.isExceeded) return 'limit-danger';
    if (limit.percent >= warnPercent) return 'limit-warn';
    return 'limit-ok';
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return this.info()?.planId === plan.id;
  }
}
