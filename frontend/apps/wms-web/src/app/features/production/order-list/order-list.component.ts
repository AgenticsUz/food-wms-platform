import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import {
  ChartComponent,
  type ApexAxisChartSeries,
  type ApexChart,
  type ApexGrid,
  type ApexLegend,
  type ApexPlotOptions,
  type ApexTooltip,
  type ApexXAxis,
  type ApexYAxis,
} from 'ng-apexcharts';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ProductionOrderStatus, type ProductionOrder } from '../production.model';
import { ProductionService } from '../production.service';
import { orderStatusClass, orderStatusKey } from '../production-status';

interface WasteChart {
  readonly series: ApexAxisChartSeries;
  readonly chart: ApexChart;
  readonly xaxis: ApexXAxis;
  readonly yaxis: ApexYAxis;
  readonly colors: string[];
  readonly grid: ApexGrid;
  readonly tooltip: ApexTooltip;
  readonly plotOptions: ApexPlotOptions;
  readonly legend: ApexLegend;
}

@Component({
  selector: 'app-order-list',
  imports: [
    DecimalPipe, DatePipe, FormsModule, TableModule, Button, Select, ChartComponent,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss',
})
export default class OrderListComponent implements OnInit {
  private readonly productionService = inject(ProductionService);
  private readonly session = inject(WmsSession);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);

  protected readonly orderStatusClass = orderStatusClass;
  protected readonly orderStatusKey = orderStatusKey;

  readonly orders = signal<ProductionOrder[]>([]);
  readonly loading = signal(true);
  readonly statusFilter = signal<ProductionOrderStatus | null>(null);
  readonly wasteChart = signal<WasteChart | null>(null);

  /** Til almashganda yorliqlar qayta hisoblansin (`language()` ga bog'liq). */
  readonly statusOptions = computed(() => {
    this.language.language();
    const tr = (key: string) => this.language.translate(key);
    return [
      { label: tr('transfer.allStatuses'), value: null },
      ...[
        ProductionOrderStatus.Draft,
        ProductionOrderStatus.InProgress,
        ProductionOrderStatus.Completed,
        ProductionOrderStatus.Cancelled,
      ].map((value) => ({ label: tr(orderStatusKey(value)), value })),
    ];
  });

  ngOnInit(): void {
    this.loadOrders();
    // Grafik `analytics.advanced` feature'i ortida: yopiq tarifda so'rov umuman
    // yuborilmaydi (eski `FeatureService` bo'sh ro'yxatda hammasini ochiq derdi).
    if (this.session.isFeatureEnabled('analytics.advanced')) {
      this.loadWasteChart();
    }
  }

  loadOrders(): void {
    this.loading.set(true);
    this.productionService.getOrders({ status: this.statusFilter() }).subscribe({
      next: (res) => {
        this.orders.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.loading.set(false),
    });
  }

  onFilterChange(value: ProductionOrderStatus | null): void {
    this.statusFilter.set(value);
    this.loadOrders();
  }

  createNew(): void {
    void this.router.navigate(['/production/orders/new']);
  }

  viewDetail(order: ProductionOrder): void {
    void this.router.navigate(['/production/orders', order.id]);
  }

  private loadWasteChart(): void {
    this.productionService.getWasteByStage(30).subscribe({
      next: (res) => {
        const data = res.data ?? [];
        if (data.length === 0) return;
        this.wasteChart.set({
          series: [{ name: this.language.translate('production.wasteQty'), data: data.map((d) => d.wastePercent) }],
          chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 },
          xaxis: { categories: data.map((d) => d.stageName) },
          colors: ['#ef4444'],
          grid: APEX_DEFAULTS.grid,
          tooltip: APEX_DEFAULTS.tooltip,
          plotOptions: { bar: { borderRadius: 4, columnWidth: '50%' } },
          legend: { show: false },
          yaxis: { labels: { formatter: (val: number) => val.toFixed(1) + '%' } },
        });
      },
      error: () => undefined,
    });
  }
}
