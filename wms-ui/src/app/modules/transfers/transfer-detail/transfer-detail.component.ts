import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { transferStatusClass, transferStatusKey, transferTypeKey } from '../../../shared/utils/transfer-enums';
import { TransferService } from '../../../core/services/transfer.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ExportService } from '../../../core/services/export.service';
import { Transfer, TransferType, TransferStatus } from '../../../core/models/transfer.model';

@Component({
  selector: 'app-transfer-detail',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, TranslocoDirective, TableModule, Button, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-detail.component.html',
  styleUrl: './transfer-detail.component.scss'
})
export default class TransferDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private transferService = inject(TransferService);
  private notify = inject(NotificationService);
  exportService = inject(ExportService);

  transfer = signal<Transfer | null>(null);
  loading = signal(true);
  confirming = signal(false);
  rejecting = signal(false);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.router.navigate(['/transfers']); return; }
    this.loadTransfer(id);
  }

  private loadTransfer(id: number) {
    this.transferService.getTransfer(id).subscribe({
      next: (res) => {
        if (res.success && res.data) this.transfer.set(res.data);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  downloadPdf() {
    const t = this.transfer();
    if (t) this.exportService.downloadPdf(t.id);
  }

  confirm() {
    const t = this.transfer();
    if (!t) return;
    this.notify.confirmAction('Confirm this transfer? Stock will be updated.', 'Confirm Transfer', () => {
      this.confirming.set(true);
      this.transferService.confirmTransfer(t.id).subscribe({
        next: () => {
          this.confirming.set(false);
          this.notify.success('Transfer confirmed');
          this.loadTransfer(t.id);
        },
        error: () => { this.confirming.set(false); }
      });
    });
  }

  reject() {
    const t = this.transfer();
    if (!t) return;
    this.notify.confirmAction('Reject this transfer?', 'Reject Transfer', () => {
      this.rejecting.set(true);
      this.transferService.rejectTransfer(t.id).subscribe({
        next: () => {
          this.rejecting.set(false);
          this.notify.success('Transfer rejected');
          this.loadTransfer(t.id);
        },
        error: () => { this.rejecting.set(false); }
      });
    });
  }

  get isPending(): boolean {
    return this.transfer()?.status === TransferStatus.Pending;
  }

  // enum → UI helperlari (template'da chaqirish uchun)
  protected transferStatusClass = transferStatusClass;
  protected transferStatusKey = transferStatusKey;
  protected transferTypeKey = transferTypeKey;

  goBack() { this.router.navigate(['/transfers']); }
}
