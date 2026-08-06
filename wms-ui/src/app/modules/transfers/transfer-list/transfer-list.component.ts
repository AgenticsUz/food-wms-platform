import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { toSignal } from '@angular/core/rxjs-interop';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { TransferService } from '../../../core/services/transfer.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { Transfer, TransferType, TransferStatus } from '../../../core/models/transfer.model';
import { toLocalDateString } from '../../../shared/utils/date.util';
import { transferStatusClass, transferStatusKey, transferTypeClass, transferTypeKey } from '../../../shared/utils/transfer-enums';

@Component({
  selector: 'app-transfer-list',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TranslocoDirective, TableModule, Button, Select, DatePicker, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-list.component.html',
  styleUrl: './transfer-list.component.scss'
})
export default class TransferListComponent implements OnInit {
  private transferService = inject(TransferService);
  private notify = inject(NotificationService);
  private router = inject(Router);
  exportService = inject(ExportService);
  private transloco = inject(TranslocoService);
  private lang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

  transfers = signal<Transfer[]>([]);
  loading = signal(true);
  typeFilter = signal<number | null>(null);
  statusFilter = signal<number | null>(null);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  typeOptions = computed(() => {
    this.lang();
    return [
      { label: this.transloco.translate('transfer.allTypes'), value: null },
      { label: this.transloco.translate('transfer.incoming'), value: TransferType.Incoming },
      { label: this.transloco.translate('transfer.outgoing'), value: TransferType.Outgoing },
      { label: this.transloco.translate('transfer.internal'), value: TransferType.Internal },
      { label: this.transloco.translate('transfer.return'), value: TransferType.Return }
    ];
  });

  statusOptions = computed(() => {
    this.lang();
    return [
      { label: this.transloco.translate('transfer.allStatuses'), value: null },
      { label: this.transloco.translate('status.pending'), value: TransferStatus.Pending },
      { label: this.transloco.translate('status.confirmed'), value: TransferStatus.Confirmed },
      { label: this.transloco.translate('status.rejected'), value: TransferStatus.Rejected },
      { label: this.transloco.translate('status.cancelled'), value: TransferStatus.Cancelled }
    ];
  });

  ngOnInit() { this.loadTransfers(); }

  loadTransfers() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = { pageSize: 100 };
    if (this.typeFilter()) params['type'] = this.typeFilter()!;
    if (this.statusFilter()) params['status'] = this.statusFilter()!;
    if (this.dateFrom()) params['from'] = toLocalDateString(this.dateFrom()!);
    if (this.dateTo()) params['to'] = toLocalDateString(this.dateTo()!);
    this.transferService.getTransfers(params).subscribe({
      next: (res) => {
        this.transfers.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  onFilterChange() { this.loadTransfers(); }
  createNew() { this.router.navigate(['/transfers/new']); }
  viewDetail(t: Transfer) { this.router.navigate(['/transfers', t.id]); }

  // enum → UI helperlari (template'da chaqirish uchun)
  protected transferStatusClass = transferStatusClass;
  protected transferStatusKey = transferStatusKey;
  protected transferTypeClass = transferTypeClass;
  protected transferTypeKey = transferTypeKey;

  isReturnType(type: TransferType): boolean {
    return type === TransferType.Return;
  }

  exportTransfers() {
    const params: Record<string, unknown> = {};
    if (this.dateFrom()) params['fromDate'] = toLocalDateString(this.dateFrom()!);
    if (this.dateTo()) params['toDate'] = toLocalDateString(this.dateTo()!);
    this.exportService.download('export/transfers', `transfers-${this.today()}.xlsx`, params);
  }
  private today() { return toLocalDateString(new Date()); }
}
