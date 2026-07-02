import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { CounterpartyService } from '../../../core/services/counterparty.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Counterparty, CounterpartyBalance, PaymentHistory, PaymentMethod } from '../../../core/models/counterparty.model';
import { Transfer } from '../../../core/models/transfer.model';
import { transferStatusClass, transferStatusKey, transferTypeClass, transferTypeKey } from '../../../shared/utils/transfer-enums';

@Component({
  selector: 'app-counterparty-detail',
  standalone: true,
  imports: [DecimalPipe, DatePipe, TableModule, Button, TranslocoDirective, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './counterparty-detail.component.html',
  styleUrl: './counterparty-detail.component.scss'
})
export default class CounterpartyDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(CounterpartyService);
  private notify = inject(NotificationService);

  // helperlar (template'da chaqirish uchun)
  protected transferStatusClass = transferStatusClass;
  protected transferStatusKey = transferStatusKey;
  protected transferTypeClass = transferTypeClass;
  protected transferTypeKey = transferTypeKey;

  counterparty = signal<Counterparty | null>(null);
  balance = signal<CounterpartyBalance | null>(null);
  transfers = signal<Transfer[]>([]);
  payments = signal<PaymentHistory[]>([]);
  loading = signal(true);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.router.navigate(['/counterparties']); return; }
    this.loadCounterparty(id);
    this.loadBalance(id);
    this.loadTransfers(id);
    this.loadPayments(id);
  }

  private loadCounterparty(id: number) {
    this.service.getCounterparty(id).subscribe({
      next: (res) => {
        if (res.success && res.data) this.counterparty.set(res.data);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load counterparty'); }
    });
  }

  private loadBalance(id: number) {
    this.service.getBalance(id).subscribe({
      next: (res) => { if (res.success && res.data) this.balance.set(res.data); }
    });
  }

  private loadTransfers(id: number) {
    this.service.getTransfers(id).subscribe({
      next: (res) => { if (res.success && res.data) this.transfers.set(res.data); }
    });
  }

  private loadPayments(id: number) {
    this.service.getPayments(id).subscribe({
      next: (res) => { if (res.success && res.data) this.payments.set(res.data); }
    });
  }

  goBack() {
    const type = this.counterparty()?.type;
    this.router.navigate(['/counterparties', type === 2 ? 'clients' : 'suppliers']);
  }

  getTypeName(type: number): string {
    switch (type) { case 1: return 'Supplier'; case 2: return 'Client'; case 3: return 'Both'; default: return 'Unknown'; }
  }

  getPaymentMethodName(method: PaymentMethod): string {
    switch (method) { case PaymentMethod.Cash: return 'Cash'; case PaymentMethod.Bank: return 'Bank'; case PaymentMethod.Card: return 'Card'; default: return 'Unknown'; }
  }
}
