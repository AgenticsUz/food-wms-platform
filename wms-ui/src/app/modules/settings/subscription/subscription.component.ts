import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { ProgressBar } from 'primeng/progressbar';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SubscriptionService } from '../../../core/services/subscription.service';
import {
  SubscriptionInfo, SubscriptionPlan, LimitRow, toLimitRows, blockedReasonKey
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

  limits = computed<LimitRow[]>(() => toLimitRows(this.info()?.limits ?? null));

  /** To'lov muddati trialdan ustun — mijoz to'lagan bo'lsa demo sanasi ahamiyatsiz. */
  usePaidPeriod = computed(() => !!this.info()?.paidUntil);
  daysLeft = this.service.daysLeft;
  inGrace = this.service.inGrace;
  isExpiringSoon = this.service.isExpiringSoon;

  graceDaysLeft = computed(() => {
    const i = this.info(); const d = this.daysLeft();
    if (!i || d === null) return 0;
    return Math.max(0, i.paymentGraceDays + d);
  });

  /** Feature'lar 25+ ta bo'lishi mumkin — modul bo'yicha guruhlab, yig'iladigan panelga qo'yamiz. */
  featuresOpen = signal(false);
  featureGroups = computed(() => {
    const groups = new Map<string, string[]>();
    for (const code of this.info()?.enabledFeatures ?? []) {
      const group = code.includes('.') ? code.split('.')[0] : 'other';
      const list = groups.get(group);
      if (list) list.push(code); else groups.set(group, [code]);
    }
    return [...groups.entries()]
      .map(([group, codes]) => ({ group, codes: codes.sort() }))
      .sort((a, b) => a.group.localeCompare(b.group));
  });

  supportPhone = computed(() => this.info()?.supportPhone || environment.supportPhone);
  supportEmail = computed(() => this.info()?.supportEmail || environment.supportEmail);

  ngOnInit() {
    this.service.fetch().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });
    this.service.plans().subscribe(res => {
      if (res.success && res.data) this.plans.set(res.data);
    });
  }

  statusKey(status: string): string {
    switch (status) {
      case 'Trial': return 'subscription.statusTrial';
      case 'Suspended': return 'subscription.statusSuspended';
      default: return 'subscription.statusActive';
    }
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Trial': return 'pill pill-warning';
      case 'Suspended': return 'pill pill-danger';
      default: return 'pill pill-success';
    }
  }

  blockedKey(info: SubscriptionInfo): string {
    return blockedReasonKey(info.blockedReason);
  }

  limitKey(limit: LimitRow): string {
    switch (limit.key) {
      case 'users': return 'subscription.limitUsers';
      case 'warehouses': return 'subscription.limitWarehouses';
      default: return 'subscription.limitTransfers';
    }
  }

  limitClass(limit: LimitRow): string {
    if (limit.isExceeded) return 'limit-danger';
    if (limit.isNearLimit) return 'limit-warn';
    return 'limit-ok';
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return this.info()?.planCode === plan.code;
  }
}
