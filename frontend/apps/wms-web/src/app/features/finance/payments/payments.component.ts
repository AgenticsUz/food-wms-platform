import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Counterparty } from '../../counterparties/counterparty.model';
import { PaymentMethod, paymentMethodKey, type PaymentHistory } from '../finance.model';
import { FinanceService } from '../finance.service';
import { parseUtc } from '../../../core/utils/date.util';

interface PaymentForm {
  /** Eskisida `0` edi (son id); Guid'da «tanlanmagan» — `null`. */
  readonly counterpartyId: string | null;
  readonly amount: number;
  readonly method: PaymentMethod;
  readonly note: string | null;
}

@Component({
  selector: 'app-payments',
  imports: [
    DecimalPipe, DatePipe, FormsModule, TableModule, Button, Select, InputNumber, Dialog, Textarea,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.scss',
})
export default class PaymentsComponent implements OnInit {
  private readonly financeSvc = inject(FinanceService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  protected readonly methodKey = paymentMethodKey;

  readonly payments = signal<PaymentHistory[]>([]);
  readonly counterparties = signal<Counterparty[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);

  readonly form = signal<PaymentForm>(this.emptyForm());

  /** Til almashganda yorliqlar qayta hisoblansin. */
  readonly methodOptions = computed(() => {
    this.language.language();
    return [PaymentMethod.Cash, PaymentMethod.Bank, PaymentMethod.Card].map((value) => ({
      label: this.language.translate(paymentMethodKey(value)),
      value,
    }));
  });

  ngOnInit(): void {
    this.loadPayments();
    this.financeSvc.getCounterparties().subscribe({
      next: (res) => this.counterparties.set(res.data ?? []),
    });
  }

  loadPayments(): void {
    this.loading.set(true);
    this.financeSvc.getPayments().subscribe({
      next: (res) => {
        this.payments.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(this.emptyForm());
    this.dialogVisible.set(true);
  }

  save(): void {
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
    this.financeSvc
      .recordPayment({ counterpartyId: f.counterpartyId, transferId: null, amount: f.amount, method: f.method, note: f.note })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success('Payment recorded');
          this.loadPayments();
        },
        error: () => this.saving.set(false),
      });
  }

  methodStatus(method: PaymentMethod): string {
    switch (method) {
      case PaymentMethod.Cash:
        return 'Confirmed';
      case PaymentMethod.Bank:
        return 'InProgress';
      case PaymentMethod.Card:
        return 'Pending';
      default:
        return 'Neutral';
    }
  }

  updateForm<K extends keyof PaymentForm>(field: K, value: PaymentForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  private emptyForm(): PaymentForm {
    return { counterpartyId: null, amount: 0, method: PaymentMethod.Cash, note: null };
  }

  /**
   * Serverdagi vaqt UTC'da keladi va `Z` siz kelishi mumkin — `new Date` uni
   * mahalliy deb o'qib 5 soat siljitardi (`core/utils/date.util`).
   */
  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }

}
