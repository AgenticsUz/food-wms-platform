import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { NotificationService } from '../../../core/notify/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { localDayRangeToUtc, toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Counterparty } from '../../counterparties/counterparty.model';
import { TransactionType, type Transaction, type TransactionCreateDto } from '../finance.model';
import { FinanceService } from '../finance.service';

interface TransactionForm {
  readonly type: TransactionType;
  readonly counterpartyId: string | null;
  readonly amount: number;
  readonly description: string | null;
}

@Component({
  selector: 'app-transactions',
  imports: [
    DecimalPipe, DatePipe, FormsModule, TableModule, Button, Select, InputNumber, Dialog, DatePicker, Textarea,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss',
})
export default class TransactionsComponent implements OnInit {
  private readonly financeSvc = inject(FinanceService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  readonly exportService = inject(ExportService);

  readonly transactions = signal<Transaction[]>([]);
  readonly counterparties = signal<Counterparty[]>([]);
  readonly loading = signal(true);
  readonly typeFilter = signal<TransactionType | null>(null);
  readonly dateFrom = signal<Date | null>(null);
  readonly dateTo = signal<Date | null>(null);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);

  /** Til almashganda yorliqlar qayta hisoblansin. */
  readonly typeOptions = computed(() => {
    this.language.language();
    return [
      { label: this.language.translate('finance.allTypes'), value: null },
      { label: this.language.translate('finance.income'), value: TransactionType.Income },
      { label: this.language.translate('finance.expense'), value: TransactionType.Expense },
    ];
  });
  readonly formTypeOptions = computed(() => this.typeOptions().slice(1));

  readonly form = signal<TransactionForm>(this.emptyForm());
  readonly formDate = signal<Date>(new Date());

  ngOnInit(): void {
    this.loadTransactions();
    this.financeSvc.getCounterparties().subscribe({
      next: (res) => this.counterparties.set(res.data ?? []),
    });
  }

  loadTransactions(): void {
    this.loading.set(true);
    const range = this.dateRange();
    this.financeSvc.getTransactions({ type: this.typeFilter(), from: range.from, to: range.to }).subscribe({
      next: (res) => {
        this.transactions.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.loading.set(false),
    });
  }

  setTypeFilter(value: TransactionType | null): void {
    this.typeFilter.set(value);
    this.loadTransactions();
  }

  setDateFrom(value: Date | null): void {
    this.dateFrom.set(value);
    this.loadTransactions();
  }

  setDateTo(value: Date | null): void {
    this.dateTo.set(value);
    this.loadTransactions();
  }

  openNew(): void {
    this.form.set(this.emptyForm());
    this.formDate.set(new Date());
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.amount || f.amount <= 0) {
      this.notify.warn('Amount must be greater than 0');
      return;
    }

    // Tranzaksiya sanasi — kalendar kuni (vaqt nuqtasi emas): `toLocalDateString`.
    const dto: TransactionCreateDto = {
      type: f.type,
      counterpartyId: f.counterpartyId,
      transferId: null,
      amount: f.amount,
      description: f.description,
      date: toLocalDateString(this.formDate()),
    };

    this.saving.set(true);
    this.financeSvc.createTransaction(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success('Transaction created');
        this.loadTransactions();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteTransaction(tx: Transaction): void {
    this.notify.confirmDelete('Delete this transaction?', () => {
      this.financeSvc.deleteTransaction(tx.id).subscribe({
        next: () => {
          this.notify.success('Transaction deleted');
          this.loadTransactions();
        },
        error: () => undefined,
      });
    });
  }

  typeKey(type: TransactionType): string {
    return type === TransactionType.Income ? 'finance.income' : 'finance.expense';
  }

  typeStatus(type: TransactionType): string {
    return type === TransactionType.Income ? 'Income' : 'Expense';
  }

  updateForm<K extends keyof TransactionForm>(field: K, value: TransactionForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  exportTransactions(): void {
    const range = this.dateRange();
    this.exportService.download('export/transactions', `transactions-${toLocalDateString(new Date())}.xlsx`, {
      fromDate: range.from,
      toDate: range.to,
    });
  }

  /**
   * Filtr kunlari → UTC vaqt nuqtalari. Eski kod `from=YYYY-MM-DD` yuborardi va
   * server uni UTC yarim tuni deb o'qirdi: Toshkentda kunning birinchi 5 soatidagi
   * yozuvlar «bugun» filtridan tushib qolardi (`localDayRangeToUtc` izohi).
   */
  private dateRange(): { from: string | undefined; to: string | undefined } {
    const from = this.dateFrom();
    const to = this.dateTo();
    return {
      from: from ? localDayRangeToUtc(from, from).from : undefined,
      to: to ? localDayRangeToUtc(to, to).to : undefined,
    };
  }

  private emptyForm(): TransactionForm {
    return { type: TransactionType.Income, counterpartyId: null, amount: 0, description: null };
  }
}
