import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../../shared/services/notification.service';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { FinanceService } from '../../../core/services/finance.service';
import { ExportService } from '../../../core/services/export.service';
import { CounterpartyService } from '../../../core/services/counterparty.service';
import { Transaction, TransactionCreateDto, TransactionType } from '../../../core/models/finance.model';
import { Counterparty } from '../../../core/models/counterparty.model';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [
    DecimalPipe, DatePipe, FormsModule, TableModule, Button,
    Select, InputNumber, Dialog, DatePicker, Textarea,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, HasPermissionDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss'
})
export default class TransactionsComponent implements OnInit {
  private financeSvc = inject(FinanceService);
  private counterpartySvc = inject(CounterpartyService);
  private notify = inject(NotificationService);
  exportService = inject(ExportService);
  private transloco = inject(TranslocoService);
  private lang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

  transactions = signal<Transaction[]>([]);
  counterparties = signal<Counterparty[]>([]);
  loading = signal(true);
  search = signal('');
  typeFilter = signal<number | null>(null);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  typeOptions = computed(() => {
    this.lang();
    return [
      { label: this.transloco.translate('finance.allTypes'), value: null },
      { label: this.transloco.translate('finance.income'), value: 1 },
      { label: this.transloco.translate('finance.expense'), value: 2 }
    ];
  });

  form = signal<TransactionCreateDto & { id?: number }>({
    type: TransactionType.Income,
    counterpartyId: null,
    transferId: null,
    amount: 0,
    description: null,
    date: new Date().toISOString().split('T')[0]
  });

  formDate = signal<Date>(new Date());

  ngOnInit() {
    this.loadTransactions();
    this.loadCounterparties();
  }

  loadTransactions() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = {};
    if (this.typeFilter()) params['type'] = this.typeFilter()!;
    if (this.dateFrom()) params['from'] = this.dateFrom()!.toISOString().split('T')[0];
    if (this.dateTo()) params['to'] = this.dateTo()!.toISOString().split('T')[0];
    this.financeSvc.getTransactions(params).subscribe({
      next: (res) => {
        this.transactions.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load transactions');
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

  onFilterChange() {
    this.loadTransactions();
  }

  openNew() {
    this.form.set({
      type: TransactionType.Income,
      counterpartyId: null,
      transferId: null,
      amount: 0,
      description: null,
      date: new Date().toISOString().split('T')[0]
    });
    this.formDate.set(new Date());
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.amount || f.amount <= 0) {
      this.notify.warn('Amount must be greater than 0');
      return;
    }

    this.saving.set(true);
    const dto: TransactionCreateDto = {
      type: f.type,
      counterpartyId: f.counterpartyId,
      transferId: f.transferId,
      amount: f.amount,
      description: f.description,
      date: this.formDate().toISOString()
    };

    this.financeSvc.createTransaction(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success('Transaction created');
        this.loadTransactions();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to create transaction');
      }
    });
  }

  deleteTransaction(t: Transaction) {
    this.notify.confirmDelete(`Delete this transaction?`, () => {
      this.financeSvc.deleteTransaction(t.id).subscribe({
        next: () => {
          this.notify.success('Transaction deleted');
          this.loadTransactions();
        },
        error: () => this.notify.error('Failed to delete transaction')
      });
    });
  }

  getTypeName(type: TransactionType): string {
    return type === TransactionType.Income
      ? this.transloco.translate('finance.income')
      : this.transloco.translate('finance.expense');
  }

  getTypeStatus(type: TransactionType): string {
    return type === TransactionType.Income ? 'Confirmed' : 'Cancelled';
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }

  exportTransactions() {
    const params: Record<string, unknown> = {};
    if (this.dateFrom()) params['fromDate'] = this.dateFrom()!.toISOString().split('T')[0];
    if (this.dateTo()) params['toDate'] = this.dateTo()!.toISOString().split('T')[0];
    this.exportService.download('export/transactions', `transactions-${this.today()}.xlsx`, params);
  }
  private today() { return new Date().toISOString().split('T')[0]; }
}
