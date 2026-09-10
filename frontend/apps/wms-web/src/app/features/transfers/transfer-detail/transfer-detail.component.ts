import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { shortTransferId, transferStatusClass, transferStatusKey, transferTypeKey } from '../transfer-enums';
import { TransferStatus, type Transfer } from '../transfer.model';
import { TransferService } from '../transfer.service';

/** Transfer tafsiloti, tasdiqlash/rad etish, PDF (eski `transfers/transfer-detail`). */
@Component({
  selector: 'app-transfer-detail',
  imports: [DecimalPipe, DatePipe, RouterLink, TranslocoDirective, TableModule, Button, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-detail.component.html',
  styleUrl: './transfer-detail.component.scss',
})
export default class TransferDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transferService = inject(TransferService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);
  readonly exportService = inject(ExportService);

  protected readonly transferStatusClass = transferStatusClass;
  protected readonly transferStatusKey = transferStatusKey;
  protected readonly shortId = shortTransferId;

  readonly transfer = signal<Transfer | null>(null);
  readonly loading = signal(true);
  readonly confirming = signal(false);
  readonly rejecting = signal(false);

  readonly isPending = computed(() => this.transfer()?.status === TransferStatus.Pending);
  /**
   * PDF faqat tasdiqlangan transferda (eski qoida) VA `export.pdf` feature'i
   * yoqilganda: feature'lar fail-closed, o'chiq tarifda tugma 403 qaytarardi.
   */
  readonly canDownloadPdf = computed(
    () => this.transfer()?.status === TransferStatus.Confirmed && this.session.isFeatureEnabled('export.pdf')
  );
  readonly typeKey = computed(() => {
    const tr = this.transfer();
    return tr ? transferTypeKey(tr.type) : '';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/transfers']);
      return;
    }
    this.loadTransfer(id);
  }

  private loadTransfer(id: string): void {
    this.transferService.getTransfer(id).subscribe({
      next: (res) => {
        if (res.success && res.data) this.transfer.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  downloadPdf(): void {
    const t = this.transfer();
    if (t) this.exportService.downloadPdf(t.id);
  }

  confirm(): void {
    const t = this.transfer();
    if (!t) return;
    this.notify.confirmAction(
      this.language.translate('transfer.confirmMsg'),
      this.language.translate('transfer.confirmTransfer'),
      () => {
        this.confirming.set(true);
        this.transferService.confirmTransfer(t.id).subscribe({
          next: () => {
            this.confirming.set(false);
            this.notify.success('Transfer confirmed');
            this.loadTransfer(t.id);
          },
          error: () => this.confirming.set(false),
        });
      }
    );
  }

  reject(): void {
    const t = this.transfer();
    if (!t) return;
    this.notify.confirmAction(
      this.language.translate('transfer.rejectMsg'),
      this.language.translate('transfer.rejectTransfer'),
      () => {
        this.rejecting.set(true);
        this.transferService.rejectTransfer(t.id).subscribe({
          next: () => {
            this.rejecting.set(false);
            this.notify.success('Transfer rejected');
            this.loadTransfer(t.id);
          },
          error: () => this.rejecting.set(false),
        });
      }
    );
  }

  goBack(): void {
    void this.router.navigate(['/transfers']);
  }
}
