import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ChartComponent } from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../core/auth/wms-session';
import { APEX_DEFAULTS } from '../../core/config/apex-defaults';
import { ExportService } from '../../core/services/export.service';
import { localDayRangeToUtc, toLocalDateString } from '../../core/utils/date.util';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { transferStatusClass, transferStatusKey, transferTypeClass, transferTypeKey } from '../transfers/transfer-enums';
import type { Transfer } from '../transfers/transfer.model';
import type { DashboardSummaryDto, ExtendedDashboardSummaryDto } from './analytics.model';
import { AnalyticsService } from './analytics.service';
import { axisSeries, type ChartConfig } from './chart-config';

type ReportKind = 'finance' | 'stock' | 'transfers' | 'debtors';
type QuickRange = 'today' | 'thisWeek' | 'thisMonth' | 'lastMonth' | 'thisYear';

/**
 * Bosh sahifa (eski `dashboard/dashboard.component`).
 *
 * Eskisi kartani ham, so'rovni ham MODULGA bog'lagan edi: plani Moliya yoki
 * Ishlab chiqarishni o'z ichiga olmagan mijoz birinchi ekranda qizil «modul
 * planingizga kirmaydi» xatosini ko'rardi. Yangi qobiqda feature'lar ham
 * fail-closed (bo'sh ro'yxat = hammasi yopiq) va marshrutda guard yo'q —
 * shuning uchun har so'rov o'z backend darvozasining (modul + feature +
 * ruxsat) aynan nusxasiga bog'langan: darvoza yopiq bo'lsa so'rov yuborilmaydi,
 * karta bo'sh holatini ko'rsatadi yoki umuman chiqmaydi.
 */
@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, DatePipe, FormsModule, ChartComponent, TableModule, Button, DatePicker, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export default class DashboardComponent implements OnInit {
  private readonly analyticsService = inject(AnalyticsService);
  private readonly session = inject(WmsSession);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  readonly exportService = inject(ExportService);

  readonly hasFinance = computed(() => this.session.isModuleEnabled('FINANCE'));
  readonly hasProduction = computed(() => this.session.isModuleEnabled('PRODUCTION'));

  /** `analytics/*` controller'ining o'zi `dashboard.view` ostida. */
  private readonly canDashboard = computed(() => this.session.can('dashboard.view'));
  private readonly hasAdvanced = computed(() => this.session.isFeatureEnabled('analytics.advanced'));
  /** `production/plan-vs-actual`: PRODUCTION + `analytics.advanced`. */
  readonly canPlanVsActual = computed(() => this.canDashboard() && this.hasProduction() && this.hasAdvanced());
  /** `transfers/daily`: TRANSFERS + `analytics.advanced`. */
  private readonly canDailyTransfers = computed(
    () => this.canDashboard() && this.session.isModuleEnabled('TRANSFERS') && this.hasAdvanced()
  );
  /** «So'nggi transferlar» — `/transfers`: TRANSFERS + `transfers.view`. */
  private readonly canRecentTransfers = computed(
    () => this.session.isModuleEnabled('TRANSFERS') && this.session.can('transfers.view')
  );

  /** Tezkor hisobotlar — `export/*` darvozalarining nusxasi (+ `export.excel`). */
  readonly reports = computed(() => {
    const excel = this.session.isFeatureEnabled('export.excel');
    const module = (code: string): boolean => this.session.isModuleEnabled(code);
    return {
      finance: excel && module('FINANCE') && this.session.can('finance.view'),
      stock:
        excel &&
        (module('WAREHOUSE_RAW') || module('WAREHOUSE_FINISHED')) &&
        this.session.can('warehouse.view'),
      transfers: excel && module('TRANSFERS') && this.session.can('transfers.view'),
      debtors: excel && (module('SUPPLIERS') || module('CLIENTS')) && this.session.can('partners.view'),
    };
  });

  readonly userName = this.session.fullName;
  readonly today = new Date();

  protected readonly transferStatusClass = transferStatusClass;
  protected readonly transferStatusKey = transferStatusKey;
  protected readonly transferTypeClass = transferTypeClass;
  protected readonly transferTypeKey = transferTypeKey;

  readonly loading = signal(true);
  readonly summary = signal<DashboardSummaryDto | null>(null);
  readonly extSummary = signal<ExtendedDashboardSummaryDto | null>(null);
  readonly extLoading = signal(true);
  readonly recentTransfers = signal<Transfer[]>([]);
  readonly selectedDays = signal(7);

  readonly fromDate = signal<Date | null>(null);
  readonly toDate = signal<Date | null>(null);

  readonly planVsActualChart = signal<ChartConfig | null>(null);
  readonly dailyTransfersChart = signal<ChartConfig | null>(null);
  readonly productDistributionChart = signal<ChartConfig | null>(null);
  readonly monthlyChart = signal<ChartConfig | null>(null);
  readonly monthlyLoading = signal(true);

  ngOnInit(): void {
    this.loadSummary();
    this.loadExtendedSummary();
    this.loadAllCharts();
    this.loadRecentTransfers();
    this.loadMonthlyComparison();
  }

  // --- Tezkor oraliqlar ---
  changeDays(days: number): void {
    this.selectedDays.set(days);
    this.fromDate.set(null);
    this.toDate.set(null);
    this.planVsActualChart.set(null);
    this.dailyTransfersChart.set(null);
    this.loadAllCharts();
  }

  setQuickRange(range: QuickRange): void {
    const now = new Date();
    let from: Date;
    let to = new Date(now);

    switch (range) {
      case 'today':
        from = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        break;
      case 'thisWeek':
        from = new Date(now);
        from.setDate(now.getDate() - now.getDay());
        break;
      case 'thisMonth':
        from = new Date(now.getFullYear(), now.getMonth(), 1);
        break;
      case 'lastMonth':
        from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        to = new Date(now.getFullYear(), now.getMonth(), 0);
        break;
      case 'thisYear':
        from = new Date(now.getFullYear(), 0, 1);
        break;
    }

    this.fromDate.set(from);
    this.toDate.set(to);
    this.applyDateRange();
  }

  applyDateRange(): void {
    this.loadExtendedSummary();
  }

  resetDateRange(): void {
    this.fromDate.set(null);
    this.toDate.set(null);
    this.loadExtendedSummary();
  }

  // --- Yuklovchilar ---
  private loadSummary(): void {
    if (!this.canDashboard()) {
      this.loading.set(false);
      return;
    }
    this.analyticsService.getSummary().subscribe({
      next: (res) => {
        if (res.success && res.data) this.summary.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private loadExtendedSummary(): void {
    if (!this.canDashboard()) {
      this.extLoading.set(false);
      return;
    }
    this.extLoading.set(true);
    const range = this.utcRange();
    this.analyticsService.getExtendedSummary(range.fromDate, range.toDate).subscribe({
      next: (res) => {
        if (res.success && res.data) this.extSummary.set(res.data);
        this.extLoading.set(false);
      },
      error: () => this.extLoading.set(false),
    });
  }

  private loadAllCharts(): void {
    this.loadPlanVsActual();
    this.loadDailyTransfers();
    this.loadProductDistribution();
  }

  private loadPlanVsActual(): void {
    if (!this.canPlanVsActual()) return;
    this.analyticsService.getProductionPlanVsActual(this.selectedDays()).subscribe({
      next: (res) => {
        const data = res.success && res.data ? res.data : [];
        if (data.length === 0) return;
        const dates = [...new Set(data.map((d) => d.date))];
        const sumOn = (date: string, pick: (d: (typeof data)[number]) => number): number =>
          data.filter((d) => d.date === date).reduce((sum, d) => sum + pick(d), 0);
        this.planVsActualChart.set({
          series: axisSeries([
            { name: this.language.translate('kpi.planned'), data: dates.map((date) => sumOn(date, (d) => d.planned)) },
            { name: this.language.translate('kpi.actual'), data: dates.map((date) => sumOn(date, (d) => d.actual)) },
          ]),
          chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 280 },
          colors: APEX_DEFAULTS.colors,
          grid: APEX_DEFAULTS.grid,
          tooltip: APEX_DEFAULTS.tooltip,
          xaxis: {
            categories: dates.map((d) =>
              new Date(d).toLocaleDateString('en', { weekday: 'short', month: 'short', day: 'numeric' })
            ),
          },
          stroke: { curve: 'smooth', width: 3 },
          markers: { size: 4 },
          legend: { position: 'top' },
        });
      },
    });
  }

  private loadDailyTransfers(): void {
    if (!this.canDailyTransfers()) return;
    this.analyticsService.getDailyTransfers(this.selectedDays()).subscribe({
      next: (res) => {
        const data = res.success && res.data ? res.data : [];
        if (data.length === 0) return;
        this.dailyTransfersChart.set({
          series: axisSeries([
            { name: this.language.translate('dashboard.incoming'), data: data.map((d) => d.incomingTotal) },
            { name: this.language.translate('dashboard.outgoing'), data: data.map((d) => d.outgoingTotal) },
          ]),
          chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 },
          colors: ['#6366f1', '#f59e0b'],
          grid: APEX_DEFAULTS.grid,
          tooltip: APEX_DEFAULTS.tooltip,
          xaxis: {
            categories: data.map((d) => new Date(d.date).toLocaleDateString('en', { weekday: 'short', day: 'numeric' })),
          },
          plotOptions: { bar: { borderRadius: 4, columnWidth: '60%' } },
          legend: { position: 'top' },
        });
      },
    });
  }

  private loadProductDistribution(): void {
    if (!this.canDashboard()) return;
    this.analyticsService.getProductDistribution().subscribe({
      next: (res) => {
        const data = res.success && res.data ? res.data : [];
        if (data.length === 0) return;
        this.productDistributionChart.set({
          series: data.map((d) => d.totalStock),
          chart: { ...APEX_DEFAULTS.chart, type: 'donut', height: 240 },
          labels: data.map((d) => d.productName),
          colors: APEX_DEFAULTS.colors,
          tooltip: APEX_DEFAULTS.tooltip,
          legend: { position: 'bottom' },
          plotOptions: {
            pie: {
              donut: {
                size: '65%',
                labels: { show: true, total: { show: true, label: this.language.translate('common.total') } },
              },
            },
          },
        });
      },
    });
  }

  private loadMonthlyComparison(): void {
    if (!this.hasFinance() || !this.canDashboard()) {
      this.monthlyLoading.set(false);
      return;
    }
    this.monthlyLoading.set(true);
    this.analyticsService.getMonthlyComparison().subscribe({
      next: (res) => {
        const data = res.success && res.data ? res.data : [];
        if (data.length > 0) {
          this.monthlyChart.set({
            series: axisSeries([
              { name: this.language.translate('finance.income'), type: 'column', data: data.map((d) => d.income) },
              { name: this.language.translate('finance.expense'), type: 'column', data: data.map((d) => d.expense) },
              { name: this.language.translate('finance.netAmount'), type: 'line', data: data.map((d) => d.net) },
            ]),
            chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 300, stacked: false },
            colors: ['#10b981', '#ef4444', '#6366f1'],
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            xaxis: { categories: data.map((d) => d.month) },
            stroke: { width: [0, 0, 3], curve: 'smooth' },
            plotOptions: { bar: { borderRadius: 4, columnWidth: '50%' } },
            legend: { position: 'top' },
            yaxis: { labels: { formatter: (val: number) => (val >= 1000 ? `${(val / 1000).toFixed(0)}k` : `${val}`) } },
          });
        }
        this.monthlyLoading.set(false);
      },
      error: () => this.monthlyLoading.set(false),
    });
  }

  private loadRecentTransfers(): void {
    if (!this.canRecentTransfers()) return;
    this.analyticsService.getRecentTransfers(5).subscribe({
      next: (res) => {
        if (res.success && res.data) this.recentTransfers.set(res.data);
      },
    });
  }

  // --- Eksport ---
  exportReport(kind: ReportKind): void {
    const today = toLocalDateString(new Date());
    const range = this.utcRange();
    switch (kind) {
      case 'finance':
        this.exportService.download('export/transactions', `finance-${today}.xlsx`, range);
        break;
      case 'stock':
        this.exportService.download('export/stock', `stock-${today}.xlsx`);
        break;
      case 'transfers':
        this.exportService.download('export/transfers', `transfers-${today}.xlsx`, range);
        break;
      case 'debtors':
        this.exportService.download('export/counterparties', `debtors-${today}.xlsx`);
        break;
    }
  }

  navigateToTransfer(id: string): void {
    void this.router.navigate(['/transfers', id]);
  }

  greetingKey(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'dashboard.goodMorning';
    if (hour < 18) return 'dashboard.goodAfternoon';
    return 'dashboard.goodEvening';
  }

  /**
   * Tanlangan kunlar UTC vaqt nuqtalariga: `dashboard-summary` `>= from && <= to`
   * bilan solishtiradi. Eskisi `YYYY-MM-DD` yuborardi — «gacha» kuni UTC yarim
   * tunda kesilib, butun kun hisobotdan tushib qolardi.
   */
  private utcRange(): { fromDate?: string; toDate?: string } {
    const from = this.fromDate();
    const to = this.toDate();
    return {
      fromDate: from ? localDayRangeToUtc(from, from).from : undefined,
      toDate: to ? localDayRangeToUtc(to, to).to : undefined,
    };
  }
}
