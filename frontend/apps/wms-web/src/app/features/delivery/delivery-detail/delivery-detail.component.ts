import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { deliveryStatusClass, deliveryStatusKey, stopStatusClass, stopStatusKey } from '../delivery-enums';
import {
  DeliveryStatus,
  DeliveryStopStatus,
  shortId,
  type Delivery,
  type DeliveryStop,
} from '../delivery.model';
import { DeliveryService } from '../delivery.service';

@Component({
  selector: 'app-delivery-detail',
  imports: [
    DatePipe,
    TranslocoDirective,
    TableModule,
    Button,
    PageHeaderComponent,
    StatusBadgeComponent,
    HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './delivery-detail.component.html',
  styleUrl: './delivery-detail.component.scss',
})
export default class DeliveryDetailComponent implements OnInit {
  private readonly service = inject(DeliveryService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);
  protected readonly exportService = inject(ExportService);

  protected readonly deliveryStatusClass = deliveryStatusClass;
  protected readonly deliveryStatusKey = deliveryStatusKey;
  protected readonly stopStatusClass = stopStatusClass;
  protected readonly stopStatusKey = stopStatusKey;
  protected readonly shortId = shortId;
  protected readonly DeliveryStatus = DeliveryStatus;
  protected readonly StopPending = DeliveryStopStatus.Pending;

  /**
   * Yuk xati PDF — backend `export.pdf` feature'ini talab qiladi. Feature
   * yopiq bo'lsa tugma ko'rsatilmaydi (aks holda bosilganda 403 toasti chiqardi).
   */
  protected readonly canWaybill = this.session.isFeatureEnabled('export.pdf');

  readonly delivery = signal<Delivery | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/delivery']);
      return;
    }
    this.load(id);
  }

  load(id: string): void {
    this.loading.set(true);
    this.service.getDelivery(id).subscribe({
      next: (res) => {
        this.delivery.set(res.success ? (res.data ?? null) : null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  setStatus(status: DeliveryStatus): void {
    const d = this.delivery();
    if (!d) return;
    this.service.updateStatus(d.id, status).subscribe({
      next: () => {
        this.notify.success(this.language.translate('common.success'));
        this.load(d.id);
      },
    });
  }

  deliver(stop: DeliveryStop): void {
    const d = this.delivery();
    if (!d) return;
    this.service.markDelivered(d.id, stop.id).subscribe({
      next: () => {
        this.notify.success(this.language.translate('common.success'));
        this.load(d.id);
      },
    });
  }

  fail(stop: DeliveryStop): void {
    const d = this.delivery();
    if (!d) return;
    this.service.markFailed(d.id, stop.id, null).subscribe({ next: () => this.load(d.id) });
  }

  downloadWaybill(): void {
    const d = this.delivery();
    if (!d) return;
    this.exportService.download(this.service.waybillPath(d.id), `waybill-${shortId(d.id)}.pdf`);
  }

  goBack(): void {
    void this.router.navigate(['/delivery']);
  }
}
