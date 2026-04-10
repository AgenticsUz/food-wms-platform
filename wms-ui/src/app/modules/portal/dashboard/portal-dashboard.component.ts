import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { PortalService } from '../../../core/services/portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PortalFinance } from '../../../core/models/settings.model';
import { Transfer, TransferStatus } from '../../../core/models/transfer.model';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, TableModule, Button, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-dashboard.component.html',
  styleUrl: './portal-dashboard.component.scss'
})
export default class PortalDashboardComponent implements OnInit {
  private portalService = inject(PortalService);

  finance = signal<PortalFinance | null>(null);
  transfers = signal<Transfer[]>([]);
  loading = signal(true);

  get counterpartyName(): string {
    return this.portalService.counterparty()?.name ?? 'Guest';
  }

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.portalService.getFinance().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.finance.set(res.data);
        }
      }
    });

    this.portalService.getTransfers({ page: 1, pageSize: 5 }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.transfers.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
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

  getTypeName(type: number): string {
    const map: Record<number, string> = { 1: 'Incoming', 2: 'Outgoing', 3: 'Internal', 4: 'Production' };
    return map[type] ?? 'Unknown';
  }
}
