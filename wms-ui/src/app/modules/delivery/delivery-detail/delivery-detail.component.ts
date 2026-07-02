import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { NotificationService } from '../../../shared/services/notification.service';
import { DeliveryService } from '../../../core/services/delivery.service';
import { ExportService } from '../../../core/services/export.service';
import { Delivery, DeliveryStatus, DeliveryStop } from '../../../core/models/delivery.model';
import {
  deliveryStatusClass, deliveryStatusKey, stopStatusClass, stopStatusKey
} from '../../../shared/utils/delivery-enums';

@Component({
  selector: 'app-delivery-detail',
  standalone: true,
  imports: [DatePipe, TranslocoDirective, TableModule, Button, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './delivery-detail.component.html',
  styleUrl: './delivery-detail.component.scss'
})
export default class DeliveryDetailComponent implements OnInit {
  private service = inject(DeliveryService);
  exportService = inject(ExportService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  protected deliveryStatusClass = deliveryStatusClass;
  protected deliveryStatusKey = deliveryStatusKey;
  protected stopStatusClass = stopStatusClass;
  protected stopStatusKey = stopStatusKey;
  protected DeliveryStatus = DeliveryStatus;

  delivery = signal<Delivery | null>(null);
  loading = signal(true);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.router.navigate(['/delivery']); return; }
    this.load(id);
  }

  load(id: number) {
    this.loading.set(true);
    this.service.getDelivery(id).subscribe({
      next: (res) => { this.delivery.set(res.success && res.data ? res.data : null); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  setStatus(status: DeliveryStatus) {
    const d = this.delivery();
    if (!d) return;
    this.service.updateStatus(d.id, status).subscribe({
      next: () => { this.notify.success(this.transloco.translate('common.success')); this.load(d.id); }
    });
  }

  deliver(stop: DeliveryStop) {
    const d = this.delivery();
    if (!d) return;
    this.service.markDelivered(d.id, stop.id).subscribe({
      next: () => { this.notify.success(this.transloco.translate('common.success')); this.load(d.id); }
    });
  }

  fail(stop: DeliveryStop) {
    const d = this.delivery();
    if (!d) return;
    this.service.markFailed(d.id, stop.id, null).subscribe({
      next: () => { this.load(d.id); }
    });
  }

  downloadWaybill() {
    const d = this.delivery();
    if (!d) return;
    this.exportService.download(this.service.waybillUrl(d.id), `waybill-${d.id}.pdf`);
  }

  goBack() { this.router.navigate(['/delivery']); }
}
