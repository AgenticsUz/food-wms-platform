import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Counterparty } from '../../counterparties/counterparty.model';
import {
  PaymentDirection,
  PaymentMethod,
  documentSourceIcon,
  documentSourceKey,
  paymentDirectionKey,
  paymentDirectionStatus,
  paymentMethodKey,
  type PaymentHistory,
} from '../finance.model';
import { FinanceService } from '../finance.service';
import { parseUtc, toLocalDateString } from '../../../core/utils/date.util';

interface PaymentForm {
  /** Eskisida `0` edi (son id); Guid'da «tanlanmagan» — `null`. */
  readonly counterpartyId: string | null;
  readonly amount: number;
  readonly method: PaymentMethod;
  /**
   * ⚠️ Sukut bo'yicha `null` — «hali tanlanmagan». Oldindan bittasini qo'yib
   * qo'ysak, foydalanuvchi e'tibor bermay yuborishi va pul teskari tomonga
   * yozilishi mumkin edi; server ham nol balansda taxmin qilolmaydi (P2.8).
   */
  readonly direction: PaymentDirection | null;
  readonly note: string | null;
}

/** Orqaga sana ruxsati — `WmsPermissions.DocumentsBackdate` bilan bir xil kod. */
const BACKDATE_PERMISSION = 'documents.backdate';

@Component({
  selector: 'app-payments',
  imports: [
    DecimalPipe, DatePipe, FormsModule, TableModule, Button, Select, SelectButton, InputNumber,
    DatePicker, Dialog, Textarea,
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
  private readonly session = inject(WmsSession);

  protected readonly methodKey = paymentMethodKey;
  protected readonly directionKey = paymentDirectionKey;
  protected readonly directionStatus = paymentDirectionStatus;
  protected readonly sourceIcon = documentSourceIcon;
  protected readonly sourceKey = documentSourceKey;

  readonly payments = signal<PaymentHistory[]>([]);
  readonly counterparties = signal<Counterparty[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);

  readonly form = signal<PaymentForm>(this.emptyForm());
  /** Hujjat sanasi alohida signalda — `p-datepicker` `Date` bilan ishlaydi. */
  readonly formDate = signal<Date>(new Date());

  /** Storno dialogi holati. */
  readonly reverseTarget = signal<PaymentHistory | null>(null);
  readonly reverseReason = signal('');
  readonly reversing = signal(false);

  /**
   * Kelajak sana hech kimga ruxsat emas (server ham 400 beradi) — kalendarda
   * shunchaki tanlab bo'lmasin, xato toastini kutish shart emas.
   */
  readonly today = new Date();

  /** Ruxsat yo'q bo'lsa kalendar faqat bugunni beradi (server 403 qaytaradi). */
  readonly canBackdate = computed(() => this.session.can(BACKDATE_PERMISSION));
  readonly minDate = computed(() => (this.canBackdate() ? null : this.today));

  /** Til almashganda yorliqlar qayta hisoblansin. */
  readonly methodOptions = computed(() => {
    this.language.language();
    return [PaymentMethod.Cash, PaymentMethod.Bank, PaymentMethod.Card].map((value) => ({
      label: this.language.translate(paymentMethodKey(value)),
      value,
    }));
  });

  readonly directionOptions = computed(() => {
    this.language.language();
    return [PaymentDirection.In, PaymentDirection.Out].map((value) => ({
      label: this.language.translate(paymentDirectionKey(value)),
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
    this.formDate.set(new Date());
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.counterpartyId) {
      this.warn('finance.selectCounterparty');
      return;
    }
    if (f.direction === null) {
      this.warn('finance.directionRequired');
      return;
    }
    if (!f.amount || f.amount <= 0) {
      this.warn('finance.amountRequired');
      return;
    }

    this.saving.set(true);
    this.financeSvc
      .recordPayment({
        counterpartyId: f.counterpartyId,
        transferId: null,
        amount: f.amount,
        method: f.method,
        direction: f.direction,
        // Kalendar kuni, vaqt nuqtasi emas — `toLocalDateString` (zona siljishisiz).
        documentDate: toLocalDateString(this.formDate()),
        note: f.note,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success(this.language.translate('finance.paymentRecorded'));
          this.loadPayments();
        },
        error: () => this.saving.set(false),
      });
  }

  /**
   * Qaytarish faqat ODDIY va hali qaytarilmagan to'lovda: stornoni storno qilish
   * va ikki marta qaytarish backendda ham taqiqlangan — tugmani ko'rsatib xato
   * kutish o'rniga uni yashiramiz.
   */
  canReverse(payment: PaymentHistory): boolean {
    return !payment.isReversed && payment.reversalOfId === null;
  }

  openReverse(payment: PaymentHistory): void {
    this.reverseTarget.set(payment);
    this.reverseReason.set('');
  }

  closeReverse(): void {
    this.reverseTarget.set(null);
  }

  confirmReverse(): void {
    const target = this.reverseTarget();
    const reason = this.reverseReason().trim();
    if (!target) {
      return;
    }
    if (!reason) {
      this.warn('finance.reversalReasonRequired');
      return;
    }

    this.reversing.set(true);
    this.financeSvc.reversePayment(target.id, reason).subscribe({
      next: () => {
        this.reversing.set(false);
        this.reverseTarget.set(null);
        this.notify.success(this.language.translate('finance.paymentReversed'));
        this.loadPayments();
      },
      // Xabarni (allaqachon tarjima qilingan) server beradi, toastini qobiq chiqaradi.
      error: () => this.reversing.set(false),
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

  /** Dialog «X» yoki ESC bilan yopilganda holat tozalansin. */
  onReverseVisible(visible: boolean): void {
    if (!visible) {
      this.closeReverse();
    }
  }

  private emptyForm(): PaymentForm {
    return { counterpartyId: null, amount: 0, method: PaymentMethod.Cash, direction: null, note: null };
  }

  private warn(key: string): void {
    this.notify.warn(this.language.translate(key));
  }

  /**
   * Serverdagi vaqt UTC'da keladi va `Z` siz kelishi mumkin — `new Date` uni
   * mahalliy deb o'qib 5 soat siljitardi (`core/utils/date.util`).
   */
  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }

}
