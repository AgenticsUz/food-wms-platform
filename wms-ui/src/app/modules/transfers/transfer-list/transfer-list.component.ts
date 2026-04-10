import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
  imports: [DecimalPipe, DatePipe, FormsModule, TableModule, Button, Select, DatePicker, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-list.component.html',
  styleUrl: './transfer-list.component.scss'
})
export default class TransferListComponent implements OnInit {
  private transferService = inject(TransferService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  transfers = signal<Transfer[]>([]);
  loading = signal(true);
  typeFilter = signal<number | null>(null);
  statusFilter = signal<number | null>(null);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  typeOptions = [
    { label: 'All Types', value: null },
    { label: 'Incoming', value: TransferType.Incoming },
    { label: 'Outgoing', value: TransferType.Outgoing },
    { label: 'Internal', value: TransferType.Internal }
  ];

  statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Pending', value: TransferStatus.Pending },
    { label: 'Confirmed', value: TransferStatus.Confirmed },
    { label: 'Rejected', value: TransferStatus.Rejected },
    { label: 'Cancelled', value: TransferStatus.Cancelled }
  ];

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
