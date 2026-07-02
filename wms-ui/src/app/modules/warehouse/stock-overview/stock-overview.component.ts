import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { toLocalDateString } from '../../../shared/utils/date.util';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Select } from 'primeng/select';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { NgApexchartsModule } from 'ng-apexcharts';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { Warehouse, WarehouseStockRow } from '../../../core/models/warehouse.model';
import { StockLevelDto } from '../../../core/models/analytics.model';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';

@Component({
  selector: 'app-stock-overview',
  standalone: true,
  imports: [DecimalPipe, FormsModule, TranslocoDirective, TableModule, Select, InputText, NgApexchartsModule, PageHeaderComponent, StatusBadgeComponent, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-overview.component.html',
  styleUrl: './stock-overview.component.scss'
})
export default class StockOverviewComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private analyticsService = inject(AnalyticsService);
  private notify = inject(NotificationService);
  exportService = inject(ExportService);

  warehouses = signal<Warehouse[]>([]);
  selectedWarehouseId = signal<number | null>(null);
  stock = signal<WarehouseStockRow[]>([]);
  filtered = signal<WarehouseStockRow[]>([]);
  loading = signal(true);
  search = signal('');

  stockChart = signal<Record<string, unknown> | null>(null);
  warehouseOptions = signal<{ label: string; value: number | null }[]>([]);

  ngOnInit() {
    this.loadWarehouses();
    this.loadStockChart();
  }

  private loadWarehouses() {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.warehouses.set(res.data);
          this.warehouseOptions.set([
            { label: 'All Warehouses', value: null },
            ...res.data.map(w => ({ label: w.name, value: w.id }))
          ]);
        }
        this.loadStock();
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load warehouses'); }
    });
  }

  loadStock() {
    this.loading.set(true);
    const whId = this.selectedWarehouseId();

    if (whId) {
      // Load stock for a specific warehouse
      const wh = this.warehouses().find(w => w.id === whId);
      if (!wh) { this.loading.set(false); return; }
      this.warehouseService.getStockRows(wh).subscribe({
        next: (rows) => {
          this.stock.set(rows);
          this.applyFilter();
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('Failed to load stock'); }
      });
    } else {
      // Load stock from ALL warehouses
      this.warehouseService.getAllStockRows(this.warehouses()).subscribe({
        next: (rows) => {
          this.stock.set(rows);
          this.applyFilter();
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('Failed to load stock'); }
      });
    }
  }

  applyFilter() {
    const q = this.search().toLowerCase();
    this.filtered.set(q
      ? this.stock().filter(s => s.productName.toLowerCase().includes(q) || s.warehouseName.toLowerCase().includes(q))
      : this.stock());
  }

  onSearch(value: string) { this.search.set(value); this.applyFilter(); }
  onWarehouseChange(value: number | null) { this.selectedWarehouseId.set(value); this.loadStock(); }

  isLow(s: WarehouseStockRow): boolean {
    return s.availableQuantity <= 0;
  }

  exportStock() {
    const params: Record<string, unknown> = {};
    if (this.selectedWarehouseId()) params['warehouseId'] = this.selectedWarehouseId();
    this.exportService.download('export/stock', `stock-${this.today()}.xlsx`, params);
  }
  private today() { return toLocalDateString(new Date()); }

  private loadStockChart() {
    this.analyticsService.getStockLevels().subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const data = res.data.slice(0, 15);
          this.stockChart.set({
            series: [{ name: 'Stock', data: data.map(d => d.currentStock) }],
            chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 280 },
            xaxis: { categories: data.map(d => d.productName) },
            colors: data.map(d => d.isLow ? '#ef4444' : '#6366f1'),
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            plotOptions: { bar: { borderRadius: 4, columnWidth: '60%', distributed: true } },
            legend: { show: false }
          });
        }
      }
    });
  }
}
