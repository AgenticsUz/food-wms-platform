import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { PortalService } from '../../../core/services/portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PortalFinance } from '../../../core/models/settings.model';
import { PaymentHistory, PaymentMethod } from '../../../core/models/counterparty.model';

@Component({
  selector: 'app-portal-finance',
  standalone: true,
  imports: [DecimalPipe, DatePipe, TableModule, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-finance.component.html',
  styleUrl: './portal-finance.component.scss'
})
export default class PortalFinanceComponent implements OnInit {
  private portalService = inject(PortalService);

  finance = signal<PortalFinance | null>(null);
  payments = signal<PaymentHistory[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.portalService.getFinance().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.finance.set(res.data);
        }
      }
    });

    this.portalService.getPayments().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.payments.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getPaymentMethodLabel(method: PaymentMethod): string {
    const map: Record<number, string> = {
      [PaymentMethod.Cash]: 'Cash',
      [PaymentMethod.Bank]: 'Bank',
      [PaymentMethod.Card]: 'Card'
    };
    return map[method] ?? 'Unknown';
  }

  getPaymentMethodBadge(method: PaymentMethod): string {
    const map: Record<number, string> = {
      [PaymentMethod.Cash]: 'Confirmed',
      [PaymentMethod.Bank]: 'InProgress',
      [PaymentMethod.Card]: 'Active'
    };
    return map[method] ?? 'Pending';
  }
}
