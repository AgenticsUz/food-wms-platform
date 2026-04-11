import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ProductionService } from '../../../core/services/production.service';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ProductionOrder, ProductionOrderStatus } from '../../../core/models/production.model';
import { WasteByStageDto } from '../../../core/models/analytics.model';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TableModule, Button, Select, NgApexchartsModule, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss'
})
export default class OrderListComponent implements OnInit {
  private productionService = inject(ProductionService);
  private analyticsService = inject(AnalyticsService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  orders = signal<ProductionOrder[]>([]);
  loading = signal(true);
  statusFilter = signal<number | null>(null);
  wasteChart = signal<Record<string, unknown> | null>(null);

  statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Draft', value: ProductionOrderStatus.Draft },
    { label: 'In Progress', value: ProductionOrderStatus.InProgress },
    { label: 'Completed', value: ProductionOrderStatus.Completed },
    { label: 'Cancelled', value: ProductionOrderStatus.Cancelled }
  ];

  ngOnInit() {
    this.loadOrders();
    this.loadWasteChart();
  }

  loadOrders() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = {};
    if (this.statusFilter()) params['status'] = this.statusFilter()!;
    this.productionService.getOrders(params).subscribe({
      next: (res) => {
        this.orders.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load production orders');
      }
    });
  }

  onFilterChange() {
    this.loadOrders();
  }

  createNew() {
    this.router.navigate(['/production/orders/new']);
  }

  viewDetail(order: ProductionOrder) {
    this.router.navigate(['/production/orders', order.id]);
  }

  getStatusName(status: ProductionOrderStatus): string {
    switch (status) {
      case ProductionOrderStatus.Draft: return 'Draft';
      case ProductionOrderStatus.InProgress: return 'In Progress';
      case ProductionOrderStatus.Completed: return 'Completed';
      case ProductionOrderStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getStatusKey(status: ProductionOrderStatus): string {
    switch (status) {
      case ProductionOrderStatus.Draft: return 'Pending';
      case ProductionOrderStatus.InProgress: return 'InProgress';
      case ProductionOrderStatus.Completed: return 'Completed';
      case ProductionOrderStatus.Cancelled: return 'Cancelled';
      default: return 'Neutral';
    }
  }

  private loadWasteChart() {
    this.analyticsService.getWasteByStage(30).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.wasteChart.set({
            series: [{ name: 'Waste %', data: res.data.map(d => d.wastePercent) }],
            chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 },
            xaxis: { categories: res.data.map(d => d.stageName) },
            colors: ['#ef4444'],
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            plotOptions: { bar: { borderRadius: 4, columnWidth: '50%' } },
            legend: { show: false },
            yaxis: { labels: { formatter: (val: number) => val.toFixed(1) + '%' } }
          });
        }
      }
    });
  }
}
