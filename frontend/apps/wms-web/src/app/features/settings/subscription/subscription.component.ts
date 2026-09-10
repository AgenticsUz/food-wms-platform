import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { ProgressBar } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import {
  blockedReasonKey,
  toLimitRows,
  type LimitRow,
  type SubscriptionInfo,
  type SubscriptionPlan,
} from './subscription.model';
import { SubscriptionService } from './subscription.service';

/**
 * Obuna ekrani. F6 (D9): «tarifni oshirish» so'rovi (lead) O'CHDI — plan Agentics
 * orqali sotiladi, shuning uchun sahifa faqat «biz bilan bog'laning» deydi.
 */
@Component({
  selector: 'app-subscription',
  imports: [DatePipe, DecimalPipe, TableModule, ProgressBar, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './subscription.component.html',
  styleUrl: './subscription.component.scss',
})
export default class SubscriptionComponent implements OnInit {
  private readonly service = inject(SubscriptionService);

  readonly info = this.service.info;
  readonly plans = signal<SubscriptionPlan[]>([]);
  readonly loading = signal(true);

  readonly limits = computed<LimitRow[]>(() => toLimitRows(this.info()?.limits ?? null));

  readonly usePaidPeriod = computed(() => !!this.info()?.paidUntil);
  readonly daysLeft = this.service.daysLeft;
  readonly inGrace = this.service.inGrace;
  /** Qolgan kun yorlig'i faqat muddat hali o'tmagan bo'lsa. */
  readonly showDaysLeft = computed(() => {
    const days = this.daysLeft();
    return days !== null && days >= 0;
  });

  readonly graceDaysLeft = computed(() => {
    const info = this.info();
    const days = this.daysLeft();
    if (!info || days === null) return 0;
    return Math.max(0, info.paymentGraceDays + days);
  });

  /** Feature'lar 25+ ta bo'lishi mumkin — modul bo'yicha guruhlab, yig'iladigan panelga. */
  readonly featuresOpen = signal(false);
  readonly featureGroups = computed(() => {
    const groups = new Map<string, string[]>();
    for (const code of this.info()?.enabledFeatures ?? []) {
      const group = code.includes('.') ? code.split('.')[0] : 'other';
      const list = groups.get(group);
      if (list) list.push(code);
      else groups.set(group, [code]);
    }
    return [...groups.entries()]
      .map(([group, codes]) => ({ group, codes: codes.sort() }))
      .sort((a, b) => a.group.localeCompare(b.group));
  });

  ngOnInit(): void {
    this.service.fetch().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false),
    });
    this.service.plans().subscribe((res) => {
      if (res.success && res.data) this.plans.set(res.data);
    });
  }

  statusKey(status: string): string {
    switch (status) {
      case 'Trial':
        return 'subscription.statusTrial';
      case 'Suspended':
        return 'subscription.statusSuspended';
      default:
        return 'subscription.statusActive';
    }
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Trial':
        return 'pill pill-warning';
      case 'Suspended':
        return 'pill pill-danger';
      default:
        return 'pill pill-success';
    }
  }

  blockedKey(info: SubscriptionInfo): string {
    return blockedReasonKey(info.blockedReason);
  }

  limitKey(limit: LimitRow): string {
    switch (limit.key) {
      case 'users':
        return 'subscription.limitUsers';
      case 'warehouses':
        return 'subscription.limitWarehouses';
      default:
        return 'subscription.limitTransfers';
    }
  }

  limitClass(limit: LimitRow): string {
    if (limit.isExceeded) return 'limit-row limit-danger';
    if (limit.isNearLimit) return 'limit-row limit-warn';
    return 'limit-row limit-ok';
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return this.info()?.planCode === plan.code;
  }
}
