import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { toSignal } from '@angular/core/rxjs-interop';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { DeliveryService } from '../../../core/services/delivery.service';
import { Delivery, DeliveryStatus } from '../../../core/models/delivery.model';
import { deliveryStatusClass, deliveryStatusKey } from '../../../shared/utils/delivery-enums';

@Component({
  selector: 'app-deliveries',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslocoDirective, TableModule, Button, Select, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './deliveries.component.html',
  styleUrl: '../vehicles/vehicles.component.scss'
})
export default class DeliveriesComponent implements OnInit {
  private service = inject(DeliveryService);
  private router = inject(Router);
  private transloco = inject(TranslocoService);
  private lang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

  protected deliveryStatusClass = deliveryStatusClass;
  protected deliveryStatusKey = deliveryStatusKey;

  deliveries = signal<Delivery[]>([]);
  loading = signal(true);
  statusFilter = signal<number | null>(null);

  statusOptions = computed(() => {
    this.lang();
    return [
      { label: this.transloco.translate('common.all'), value: null },
      { label: this.transloco.translate('delivery.statusPlanned'), value: DeliveryStatus.Planned },
      { label: this.transloco.translate('delivery.statusInProgress'), value: DeliveryStatus.InProgress },
      { label: this.transloco.translate('delivery.statusCompleted'), value: DeliveryStatus.Completed },
      { label: this.transloco.translate('delivery.statusCancelled'), value: DeliveryStatus.Cancelled }
    ];
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getDeliveries(this.statusFilter() ?? undefined).subscribe({
      next: (res) => { this.deliveries.set(res.success && res.data ? res.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onFilterChange() { this.load(); }
  createNew() { this.router.navigate(['/delivery/new']); }
  view(d: Delivery) { this.router.navigate(['/delivery', d.id]); }
}
