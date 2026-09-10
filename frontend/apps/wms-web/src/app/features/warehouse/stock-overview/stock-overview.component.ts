import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Select } from 'primeng/select';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { ChartComponent } from 'ng-apexcharts';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';
import { ExportService } from '../../../core/services/export.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { AnalyticsService } from '../../dashboard/analytics.service';
import { axisSeries, type ChartConfig } from '../../dashboard/chart-config';
import { injectTranslationTick } from '../translation-tick';
import type { Warehouse, WarehouseStockRow } from '../warehouse.model';
import { WarehouseService } from '../warehouse.service';

/**
 * Zaxira ko'rinishi — barcha omborlar qoldig'i + qoldiq grafigi (eski
 * `warehouse/stock-overview`).
 *
 * Feature'lar endi fail-closed (bo'sh ro'yxat = hammasi yopiq). Qoldiq
 * (`warehouse.stock`) va grafik (`analytics.advanced`) o'chiq tarifda so'rov
 * YUBORILMAYDI: aks holda har ombor uchun alohida 403 toast chiqardi.
 */
@Component({
  selector: 'app-stock-overview',
  imports: [DecimalPipe, FormsModule, TranslocoDirective, TableModule, Select, InputText, ChartComponent, PageHeaderComponent, StatusBadgeComponent, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-overview.component.html',
  styleUrl: './stock-overview.component.scss',
})
export default class StockOverviewComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly analyticsService = inject(AnalyticsService);
  private readonly session = inject(WmsSession);
  private readonly language = inject(LanguageService);
  private readonly translationTick = injectTranslationTick();
  readonly exportService = inject(ExportService);

  readonly warehouses = signal<Warehouse[]>([]);
  readonly selectedWarehouseId = signal<string | null>(null);
  readonly stock = signal<WarehouseStockRow[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly stockChart = signal<ChartConfig | null>(null);

  readonly canExport = computed(() => this.session.isFeatureEnabled('export.excel'));
  private readonly canViewStock = computed(() => this.session.isFeatureEnabled('warehouse.stock'));

  readonly warehouseOptions = computed(() => {
    this.translationTick();
    const all: { label: string; value: string | null } = {
      label: this.language.translate('warehouse.allWarehouses'),
      value: null,
    };
    return [all, ...this.warehouses().map((w) => ({ label: w.name, value: w.id }))];
  });

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase();
    return q
      ? this.stock().filter(
          (s) => s.productName.toLowerCase().includes(q) || s.warehouseName.toLowerCase().includes(q)
        )
      : this.stock();
  });

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadStockChart();
  }

  private loadWarehouses(): void {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        if (res.success && res.data) this.warehouses.set(res.data);
        this.loadStock();
      },
      error: () => this.loading.set(false),
    });
  }

  loadStock(): void {
    if (!this.canViewStock()) {
      this.stock.set([]);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    const whId = this.selectedWarehouseId();
    const selected = whId !== null ? this.warehouses().filter((w) => w.id === whId) : this.warehouses();
    this.warehouseService.getAllStockRows(selected).subscribe({
      next: (rows) => {
        this.stock.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onWarehouseChange(value: string | null): void {
    this.selectedWarehouseId.set(value);
    this.loadStock();
  }

  isLow(s: WarehouseStockRow): boolean {
    return s.availableQuantity <= 0;
  }

  exportStock(): void {
    this.exportService.download('export/stock', `stock-${toLocalDateString(new Date())}.xlsx`, {
      warehouseId: this.selectedWarehouseId(),
    });
  }

  private loadStockChart(): void {
    if (!this.session.isFeatureEnabled('analytics.advanced')) return;
    this.analyticsService.getStockLevels().subscribe({
      next: (res) => {
        if (!res.success || !res.data || res.data.length === 0) return;
        const data = res.data.slice(0, 15);
        this.stockChart.set({
          series: axisSeries([
            { name: this.language.translate('warehouse.totalQty'), data: data.map((d) => d.currentStock) },
          ]),
          chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 280 },
          xaxis: { categories: data.map((d) => d.productName) },
          colors: data.map((d) => (d.isLow ? '#ef4444' : '#6366f1')),
          grid: APEX_DEFAULTS.grid,
          tooltip: APEX_DEFAULTS.tooltip,
          plotOptions: { bar: { borderRadius: 4, columnWidth: '60%', distributed: true } },
          legend: { show: false },
        });
      },
    });
  }
}
