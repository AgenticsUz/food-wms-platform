import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';

import { parseUtc } from '../../../core/utils/date.util';
import { PortalActorKind, type PortalAgentSummary, type PortalFinance } from '../portal.model';
import { PortalService } from '../portal.service';
import { PortalStore } from '../portal.store';

/**
 * Kabinet bosh sahifasi: kontragentga — balans va aylanma, agentga — komissiya.
 *
 * ⚠️ Qaysi so'rov ketishi ROLGA qarab hal qilinadi: kontragent `agent/summary` ga
 * borsa 403 olardi (va aksincha) — ya'ni foydalanuvchi hech qachon ko'rmaydigan
 * xato toasti chiqardi.
 */
@Component({
  selector: 'app-portal-overview',
  imports: [DecimalPipe, DatePipe, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-overview.component.html',
  styleUrl: './portal-overview.component.scss',
})
export default class PortalOverviewComponent implements OnInit {
  private readonly portal = inject(PortalService);
  protected readonly store = inject(PortalStore);

  readonly finance = signal<PortalFinance | null>(null);
  readonly agent = signal<PortalAgentSummary | null>(null);
  readonly loading = signal(true);

  readonly isAgent = computed(() => this.store.me()?.kind === PortalActorKind.Agent);

  /** Qarz belgisi: musbat — men qarzdorman, manfiy — zavod menga qarzdor. */
  readonly debt = computed(() => this.finance()?.debtAmount ?? 0);

  ngOnInit(): void {
    // Kimligi aniqlangach so'raladi: rolga mos bo'lmagan yuza 403 berardi.
    this.store.ensureSafe().subscribe({
      next: (me) => {
        if (!me) {
          this.loading.set(false);
          return;
        }
        if (me.kind === PortalActorKind.Agent) {
          this.loadAgent();
        } else {
          this.loadFinance();
        }
      },
      error: () => this.loading.set(false),
    });
  }

  private loadAgent(): void {
    this.portal.getAgentSummary().subscribe({
      next: (res) => {
        this.agent.set(res.data ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private loadFinance(): void {
    this.portal.getFinance().subscribe({
      next: (res) => {
        this.finance.set(res.data ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }
}
