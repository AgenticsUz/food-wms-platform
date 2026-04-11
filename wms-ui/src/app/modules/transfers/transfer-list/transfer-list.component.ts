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
import { Transfer, TransferType, TransferStatus } from '../../../core/models/transfer.model';

@Component({
  selector: 'app-transfer-list',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TranslocoDirective, TableModule, Button, Select, DatePicker, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-list.component.html',
  styleUrl: './transfer-list.component.scss'
})
export default class TransferListComponent implements OnInit {
  private transferService = inject(TransferService);
  private notify = inject(NotificationService);
  private router = inject(Router);
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
      { label: this.transloco.translate('transfer.internal'), value: TransferType.Internal }
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
    if (this.dateFrom()) params['from'] = this.dateFrom()!.toISOString().split('T')[0];
    if (this.dateTo()) params['to'] = this.dateTo()!.toISOString().split('T')[0];
    this.transferService.getTransfers(params).subscribe({
      next: (res) => {
        this.transfers.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load transfers'); }
    });
  }

  onFilterChange() { this.loadTransfers(); }
  createNew() { this.router.navigate(['/transfers/new']); }
  viewDetail(t: Transfer) { this.router.navigate(['/transfers', t.id]); }

  getTypeName(type: TransferType): string {
    switch (type) {
      case TransferType.Incoming: return 'Incoming';
      case TransferType.Outgoing: return 'Outgoing';
      case TransferType.Internal: return 'Internal';
      case TransferType.ProductionOutput: return 'Production';
      default: return 'Unknown';
    }
  }

  getStatusName(status: TransferStatus): string {
    switch (status) {
      case TransferStatus.Pending: return 'Pending';
      case TransferStatus.Confirmed: return 'Confirmed';
      case TransferStatus.Rejected: return 'Rejected';
      case TransferStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getStatusKey(status: TransferStatus): string {
    switch (status) {
      case TransferStatus.Pending: return 'Pending';
      case TransferStatus.Confirmed: return 'Confirmed';
      case TransferStatus.Rejected: return 'Rejected';
      case TransferStatus.Cancelled: return 'Cancelled';
      default: return 'Neutral';
    }
  }
}
