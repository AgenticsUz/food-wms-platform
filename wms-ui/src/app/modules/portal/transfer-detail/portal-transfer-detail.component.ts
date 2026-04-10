import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { PortalService } from '../../../core/services/portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { Transfer, TransferType, TransferStatus } from '../../../core/models/transfer.model';

@Component({
  selector: 'app-portal-transfer-detail',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, TableModule, Button, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-transfer-detail.component.html',
  styleUrl: './portal-transfer-detail.component.scss'
})
export default class PortalTransferDetailComponent implements OnInit {
  private portalService = inject(PortalService);
  private route = inject(ActivatedRoute);

  transfer = signal<Transfer | null>(null);
  loading = signal(true);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.loadTransfer(id);
    }
  }

  private loadTransfer(id: number) {
    this.portalService.getTransfer(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.transfer.set(res.data);
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
