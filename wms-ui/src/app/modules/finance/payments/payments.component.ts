import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../../shared/services/notification.service';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { FinanceService } from '../../../core/services/finance.service';
import { CounterpartyService } from '../../../core/services/counterparty.service';
import { PaymentCreateDto } from '../../../core/models/finance.model';
import { Counterparty, PaymentHistory, PaymentMethod } from '../../../core/models/counterparty.model';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [
    DecimalPipe, DatePipe, FormsModule, TableModule, Button,
    Select, InputNumber, Dialog, Textarea,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, HasPermissionDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.scss'
})
export default class PaymentsComponent implements OnInit {
  private financeSvc = inject(FinanceService);
  private counterpartySvc = inject(CounterpartyService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);
  private lang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

  payments = signal<PaymentHistory[]>([]);
  counterparties = signal<Counterparty[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  saving = signal(false);

  form = signal<PaymentCreateDto>({
    counterpartyId: 0,
    transferId: null,
    amount: 0,
    method: PaymentMethod.Cash,
    note: null
  });

  methodOptions = computed(() => {
    this.lang();
    return [
      { label: this.transloco.translate('finance.cash'), value: PaymentMethod.Cash },
      { label: this.transloco.translate('finance.bank'), value: PaymentMethod.Bank },
      { label: this.transloco.translate('finance.card'), value: PaymentMethod.Card }
    ];
  });

  ngOnInit() {
    this.loadPayments();
    this.loadCounterparties();
  }

  loadPayments() {
    this.loading.set(true);
    this.financeSvc.getPayments().subscribe({
      next: (res) => {
        this.payments.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load payments');
      }
    });
  }

  private loadCounterparties() {
    this.counterpartySvc.getCounterparties().subscribe({
      next: (res) => {
        if (res.success && res.data) this.counterparties.set(res.data);
      }
    });
  }

  openNew() {
    this.form.set({
      counterpartyId: 0,
      transferId: null,
      amount: 0,
      method: PaymentMethod.Cash,
      note: null
    });
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.counterpartyId) {
      this.notify.warn('Please select a counterparty');
      return;
    }
    if (!f.amount || f.amount <= 0) {
      this.notify.warn('Amount must be greater than 0');
      return;
    }

    this.saving.set(true);
    this.financeSvc.recordPayment(f).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success('Payment recorded');
        this.loadPayments();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to record payment');
      }
    });
  }

  getMethodName(method: PaymentMethod): string {
    switch (method) {
      case PaymentMethod.Cash: return this.transloco.translate('finance.cash');
      case PaymentMethod.Bank: return this.transloco.translate('finance.bank');
      case PaymentMethod.Card: return this.transloco.translate('finance.card');
      default: return 'Unknown';
    }
  }

  getMethodStatus(method: PaymentMethod): string {
    switch (method) {
      case PaymentMethod.Cash: return 'Confirmed';
      case PaymentMethod.Bank: return 'InProgress';
      case PaymentMethod.Card: return 'Pending';
      default: return 'Neutral';
    }
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
