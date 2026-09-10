import { ChangeDetectionStrategy, Component, type OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TranslocoDirective } from '@jsverse/transloco';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { CounterpartyType, counterpartyTypeKey } from '../../counterparties/counterparty.model';
import type { Debt } from '../finance.model';
import { FinanceService } from '../finance.service';

/**
 * ⚠️ «Sana» ustuni YO'Q: F6 `DebtDto` da `updatedAt` (va `id`) yo'q — qarz
 * endi hisoblanadigan qiymat, alohida yozuv emas.
 */
@Component({
  selector: 'app-debts',
  imports: [DecimalPipe, TableModule, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './debts.component.html',
  styleUrl: './debts.component.scss',
})
export default class DebtsComponent implements OnInit {
  private readonly financeSvc = inject(FinanceService);

  protected readonly typeKey = counterpartyTypeKey;

  readonly debts = signal<Debt[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.financeSvc.getDebts().subscribe({
      next: (res) => {
        this.debts.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.loading.set(false),
    });
  }

  /** Tur endi SON enum (eskisida `'Supplier'` satri edi). */
  typeBadgeStatus(type: CounterpartyType): string {
    switch (type) {
      case CounterpartyType.Supplier:
        return 'Pending';
      case CounterpartyType.Client:
        return 'Confirmed';
      default:
        return 'InProgress';
    }
  }
}
