import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { FinanceService } from '../../../core/services/finance.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Debt } from '../../../core/models/finance.model';

@Component({
  selector: 'app-debts',
  standalone: true,
  imports: [DecimalPipe, DatePipe, TableModule, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './debts.component.html',
  styleUrl: './debts.component.scss'
})
export default class DebtsComponent implements OnInit {
  private financeSvc = inject(FinanceService);
  private notify = inject(NotificationService);

  debts = signal<Debt[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadDebts();
  }

  private loadDebts() {
    this.loading.set(true);
    this.financeSvc.getDebts().subscribe({
      next: (res) => {
        this.debts.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load debts');
      }
    });
  }

  getTypeBadgeStatus(type: string): string {
    switch (type) {
      case 'Supplier': return 'Pending';
      case 'Client': return 'Confirmed';
      default: return 'InProgress';
    }
  }

  getDebtDirection(amount: number): string {
    return amount >= 0 ? 'They owe us' : 'We owe them';
  }
}
