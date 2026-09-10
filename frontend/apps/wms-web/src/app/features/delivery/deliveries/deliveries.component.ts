import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { deliveryStatusClass, deliveryStatusKey } from '../delivery-enums';
import { DeliveryStatus, shortId, type Delivery } from '../delivery.model';
import { DeliveryService } from '../delivery.service';

@Component({
  selector: 'app-deliveries',
  imports: [
    DatePipe,
    FormsModule,
    TranslocoDirective,
    TableModule,
    Button,
    Select,
    PageHeaderComponent,
    StatusBadgeComponent,
    HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './deliveries.component.html',
  styleUrl: '../vehicles/vehicles.component.scss',
})
export default class DeliveriesComponent implements OnInit {
  private readonly service = inject(DeliveryService);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);

  protected readonly deliveryStatusClass = deliveryStatusClass;
  protected readonly deliveryStatusKey = deliveryStatusKey;
  protected readonly shortId = shortId;

  readonly deliveries = signal<Delivery[]>([]);
  readonly loading = signal(true);
  readonly statusFilter = signal<DeliveryStatus | null>(null);

  /** Til almashganda yorliqlar qayta hisoblanadi (`language()` — bog'liqlik). */
  readonly statusOptions = computed(() => {
    this.language.language();
    const t = (key: string) => this.language.translate(key);
    return [
      { label: t('common.all'), value: null },
      { label: t('delivery.statusPlanned'), value: DeliveryStatus.Planned },
      { label: t('delivery.statusInProgress'), value: DeliveryStatus.InProgress },
      { label: t('delivery.statusCompleted'), value: DeliveryStatus.Completed },
      { label: t('delivery.statusCancelled'), value: DeliveryStatus.Cancelled },
    ];
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getDeliveries(this.statusFilter() ?? undefined).subscribe({
      next: (res) => {
        this.deliveries.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onFilterChange(status: DeliveryStatus | null): void {
    this.statusFilter.set(status);
    this.load();
  }

  createNew(): void {
    void this.router.navigate(['/delivery/new']);
  }

  view(d: Delivery): void {
    void this.router.navigate(['/delivery', d.id]);
  }
}
