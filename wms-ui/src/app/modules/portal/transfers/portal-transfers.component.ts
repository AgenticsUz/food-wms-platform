import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { PortalService } from '../../../core/services/portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { Transfer, TransferType, TransferStatus } from '../../../core/models/transfer.model';

@Component({
  selector: 'app-portal-transfers',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, FormsModule, TableModule, Button, DatePicker, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-transfers.component.html',
  styleUrl: './portal-transfers.component.scss'
})
export default class PortalTransfersComponent implements OnInit {
  private portalService = inject(PortalService);

  transfers = signal<Transfer[]>([]);
  loading = signal(true);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  ngOnInit() {
    this.loadTransfers();
  }

  loadTransfers() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = {};
    if (this.dateFrom()) {
      params['from'] = this.dateFrom()!.toISOString();
    }
    if (this.dateTo()) {
      params['to'] = this.dateTo()!.toISOString();
    }

    this.portalService.getTransfers(params).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.transfers.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onDateChange() {
    this.loadTransfers();
  }

  getStatusLabel(status: TransferStatus): string {
    const map: Record<number, string> = {
      [TransferStatus.Pending]: 'Pending',
      [TransferStatus.Confirmed]: 'Confirmed',
      [TransferStatus.Rejected]: 'Rejected',
      [TransferStatus.Cancelled]: 'Cancelled'
    };
    return map[status] ?? 'Unknown';
  }

  getTypeName(type: TransferType): string {
    const map: Record<number, string> = {
      [TransferType.Incoming]: 'Incoming',
      [TransferType.Outgoing]: 'Outgoing',
      [TransferType.Internal]: 'Internal',
      [TransferType.ProductionOutput]: 'Production'
    };
    return map[type] ?? 'Unknown';
  }
}
