import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { ExportService } from '../../../core/services/export.service';
import { localDayRangeToUtc, toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { injectTranslationTick } from '../../warehouse/translation-tick';
import {
  shortTransferId,
  transferStatusClass,
  transferStatusKey,
  transferTypeClass,
  transferTypeKey,
} from '../transfer-enums';
import { TransferStatus, TransferType, type Transfer } from '../transfer.model';
import { TransferService } from '../transfer.service';
import { parseUtc } from '../../../core/utils/date.util';

/** Transferlar ro'yxati (eski `transfers/transfer-list`). */
@Component({
  selector: 'app-transfer-list',
  imports: [DecimalPipe, DatePipe, FormsModule, TranslocoDirective, TableModule, Button, Select, DatePicker, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-list.component.html',
  styleUrl: './transfer-list.component.scss',
})
export default class TransferListComponent implements OnInit {
  private readonly transferService = inject(TransferService);
  private readonly router = inject(Router);
  private readonly session = inject(WmsSession);
  private readonly language = inject(LanguageService);
  private readonly translationTick = injectTranslationTick();
  readonly exportService = inject(ExportService);

  protected readonly transferStatusClass = transferStatusClass;
  protected readonly transferStatusKey = transferStatusKey;
  protected readonly transferTypeClass = transferTypeClass;
  protected readonly transferTypeKey = transferTypeKey;
  protected readonly shortId = shortTransferId;

  readonly transfers = signal<Transfer[]>([]);
  readonly loading = signal(true);
  readonly typeFilter = signal<TransferType | null>(null);
  readonly statusFilter = signal<TransferStatus | null>(null);
  readonly dateFrom = signal<Date | null>(null);
  readonly dateTo = signal<Date | null>(null);

  /** Excel eksport `export.excel` feature'i ostida (fail-closed) — o'chiq bo'lsa tugma yo'q. */
  readonly canExport = computed(() => this.session.isFeatureEnabled('export.excel'));

  readonly typeOptions = computed(() => {
    this.translationTick();
    const t = (key: string): string => this.language.translate(key);
    return [
      { label: t('transfer.allTypes'), value: null },
      { label: t('transfer.incoming'), value: TransferType.Incoming },
      { label: t('transfer.outgoing'), value: TransferType.Outgoing },
      { label: t('transfer.internal'), value: TransferType.Internal },
      { label: t('transfer.return'), value: TransferType.Return },
    ];
  });

  readonly statusOptions = computed(() => {
    this.translationTick();
    const t = (key: string): string => this.language.translate(key);
    return [
      { label: t('transfer.allStatuses'), value: null },
      { label: t('status.pending'), value: TransferStatus.Pending },
      { label: t('status.confirmed'), value: TransferStatus.Confirmed },
      { label: t('status.rejected'), value: TransferStatus.Rejected },
      { label: t('status.cancelled'), value: TransferStatus.Cancelled },
    ];
  });

  ngOnInit(): void {
    this.loadTransfers();
  }

  loadTransfers(): void {
    this.loading.set(true);
    const range = this.utcRange();
    this.transferService
      .getTransfers({
        pageSize: 100,
        type: this.typeFilter(),
        status: this.statusFilter(),
        from: range.from,
        to: range.to,
      })
      .subscribe({
        next: (res) => {
          this.transfers.set(res.success && res.data ? res.data : []);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onTypeChange(value: TransferType | null): void {
    this.typeFilter.set(value);
    this.loadTransfers();
  }

  onStatusChange(value: TransferStatus | null): void {
    this.statusFilter.set(value);
    this.loadTransfers();
  }

  onDateFrom(value: Date | null): void {
    this.dateFrom.set(value);
    this.loadTransfers();
  }

  onDateTo(value: Date | null): void {
    this.dateTo.set(value);
    this.loadTransfers();
  }

  createNew(): void {
    void this.router.navigate(['/transfers/new']);
  }

  viewDetail(row: Transfer): void {
    void this.router.navigate(['/transfers', row.id]);
  }

  isReturnType(type: TransferType): boolean {
    return type === TransferType.Return;
  }

  exportTransfers(): void {
    const range = this.utcRange();
    this.exportService.download('export/transfers', `transfers-${toLocalDateString(new Date())}.xlsx`, {
      fromDate: range.from,
      toDate: range.to,
    });
  }

  /**
   * Tanlangan kunlar — UTC vaqt nuqtalari: backend `CreatedAt >= from` va
   * `<= to` qiladi. Eskisi `YYYY-MM-DD` yuborib, «gacha» kunining UTC yarim
   * tundan keyingi (ya'ni deyarli butun) qismini tashlab yuborardi.
   */
  private utcRange(): { from?: string; to?: string } {
    const from = this.dateFrom();
    const to = this.dateTo();
    return {
      from: from ? localDayRangeToUtc(from, from).from : undefined,
      to: to ? localDayRangeToUtc(to, to).to : undefined,
    };
  }

  /**
   * Serverdagi vaqt UTC'da keladi va `Z` siz kelishi mumkin — `new Date` uni
   * mahalliy deb o'qib 5 soat siljitardi (`core/utils/date.util`).
   */
  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }

}
