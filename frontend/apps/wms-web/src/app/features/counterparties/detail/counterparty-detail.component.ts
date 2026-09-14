import { ChangeDetectionStrategy, Component, type OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';

import { parseUtc } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { TelegramLinkDialogComponent, type TelegramLinkApi } from '../../../shared/components/telegram-link-dialog/telegram-link-dialog.component';
import { PortalAccountCardComponent } from '../../../shared/components/portal-account/portal-account-card.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { paymentMethodKey, type PaymentHistory } from '../../finance/finance.model';
import {
  CounterpartyType,
  counterpartyTypeKey,
  transferStatusClass,
  transferStatusKey,
  transferTypeClass,
  transferTypeKey,
  type Counterparty,
  type CounterpartyBalance,
  type CounterpartyTransfer,
} from '../counterparty.model';
import { CounterpartyService } from '../counterparty.service';

/**
 * ⚠️ Eski «Portal» kartasi o'rnida endi «STIR» kartasi: kontragent portali F6 da
 * o'chdi (D8), INN esa kontragentning oddiy maydoni bo'lib qoldi (D10).
 */
@Component({
  selector: 'app-counterparty-detail',
  imports: [DecimalPipe, DatePipe, TableModule, Button, TranslocoDirective, PageHeaderComponent, StatusBadgeComponent, TelegramLinkDialogComponent, HasPermissionDirective, PortalAccountCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './counterparty-detail.component.html',
  styleUrl: './counterparty-detail.component.scss',
})
export default class CounterpartyDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CounterpartyService);

  // Shablonda chaqiriladigan yordamchilar.
  protected readonly transferStatusClass = transferStatusClass;
  protected readonly transferStatusKey = transferStatusKey;
  protected readonly transferTypeClass = transferTypeClass;
  protected readonly transferTypeKey = transferTypeKey;
  protected readonly typeKey = counterpartyTypeKey;
  protected readonly methodKey = paymentMethodKey;

  readonly counterparty = signal<Counterparty | null>(null);

  /** Telegram ulash (TG13) — havola kartadan, menejer mijozga o'zi yuboradi. */
  readonly telegramVisible = signal(false);
  readonly telegramApi = signal<TelegramLinkApi | null>(null);

  openTelegram(): void {
    const c = this.counterparty();
    if (!c) return;
    this.telegramApi.set(this.service.telegram(c.id));
    this.telegramVisible.set(true);
  }
  readonly balance = signal<CounterpartyBalance | null>(null);
  readonly transfers = signal<CounterpartyTransfer[]>([]);
  readonly payments = signal<PaymentHistory[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/counterparties']);
      return;
    }
    this.service.getCounterparty(id).subscribe({
      next: (res) => {
        this.counterparty.set(res.data ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.service.getBalance(id).subscribe({
      next: (res) => this.balance.set(res.data ?? null),
    });
    this.service.getTransfers(id).subscribe({
      next: (res) => this.transfers.set(res.data ?? []),
      // Modul/ruxsat yo'q — tarix bo'sh qoladi (servis izohi).
      error: () => undefined,
    });
    this.service.getPayments(id).subscribe({
      next: (res) => this.payments.set(res.data ?? []),
    });
  }

  /**
   * Serverdagi vaqt UTC'da keladi — `Z` siz satrni `new Date` mahalliy deb
   * o'qib 5 soat siljitardi (`date.util` izohi). Tasdiqlangan payt bo'lsa u,
   * aks holda yaratilgan payt ko'rsatiladi.
   */
  transferMoment(row: CounterpartyTransfer): Date | null {
    return parseUtc(row.confirmedAt ?? row.createdAt);
  }

  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }

  /** Yoyilgan qatordagi sarlavha uchun — necha tur tovar. */
  itemCount(row: CounterpartyTransfer): number {
    return row.items?.length ?? 0;
  }

  goBack(): void {
    const type = this.counterparty()?.type;
    void this.router.navigate(['/counterparties', type === CounterpartyType.Client ? 'clients' : 'suppliers']);
  }
}
