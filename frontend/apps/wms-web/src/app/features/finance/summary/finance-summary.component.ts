import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ChartComponent,
  type ApexAxisChartSeries,
  type ApexChart,
  type ApexFill,
  type ApexGrid,
  type ApexLegend,
  type ApexPlotOptions,
  type ApexStroke,
  type ApexTooltip,
  type ApexXAxis,
} from 'ng-apexcharts';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';
import { CurrencyService } from '../../../core/services/currency.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { FinanceSummary } from '../finance.model';
import { FinanceService } from '../finance.service';

interface AreaChart {
  readonly series: ApexAxisChartSeries;
  readonly chart: ApexChart;
  readonly xaxis: ApexXAxis;
  readonly colors: string[];
  readonly grid: ApexGrid;
  readonly tooltip: ApexTooltip;
  readonly fill: ApexFill;
  readonly stroke: ApexStroke;
  readonly legend: ApexLegend;
}

interface BarChart {
  readonly series: ApexAxisChartSeries;
  readonly chart: ApexChart;
  readonly xaxis: ApexXAxis;
  readonly colors: string[];
  readonly grid: ApexGrid;
  readonly tooltip: ApexTooltip;
  readonly plotOptions: ApexPlotOptions;
  readonly legend: ApexLegend;
}

@Component({
  selector: 'app-finance-summary',
  imports: [DecimalPipe, FormsModule, ChartComponent, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './finance-summary.component.html',
  styleUrl: './finance-summary.component.scss',
})
export default class FinanceSummaryComponent implements OnInit {
  private readonly financeSvc = inject(FinanceService);
  private readonly session = inject(WmsSession);
  private readonly language = inject(LanguageService);
  readonly currencyService = inject(CurrencyService);

  /** ISO valyuta kodlari — tarjima qilinmaydi (ma'lumot, matn emas). */
  readonly currencies: readonly string[] = ['USD', 'EUR', 'RUB', 'CNY', 'GBP'];
  readonly baseCurrency = 'UZS';

  readonly summary = signal<FinanceSummary | null>(null);
  readonly loading = signal(true);
  readonly chartConfig = signal<AreaChart | null>(null);
  readonly debtorsChart = signal<BarChart | null>(null);

  // Valyuta kalkulyatori
  readonly selectedCurrency = signal('USD');
  readonly foreignAmount = signal<number | null>(null);
  readonly uzsAmount = signal<number | null>(null);
  readonly customRate = signal<number | null>(null);

  readonly currentRate = computed(
    () => this.customRate() || this.currencyService.rates()[this.selectedCurrency()] || 0
  );

  readonly lastUpdatedFormatted = computed(() => {
    const d = this.currencyService.lastUpdated();
    if (!d) return '';
    return new Date(d).toLocaleDateString('uz-UZ', {
      day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit',
    });
  });

  ngOnInit(): void {
    this.financeSvc.getSummary().subscribe({
      next: (res) => {
        if (res.success && res.data) this.summary.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    // Grafiklar `analytics.advanced` ortida — yopiq tarifda so'rov yuborilmaydi,
    // bloklar esa «ma'lumot yo'q» holatida qoladi (eski ko'rinish saqlanadi).
    if (this.session.isFeatureEnabled('analytics.advanced')) {
      this.loadChart();
      this.loadDebtorsChart();
    }
    // Kurslar odatda topbar vidjeti orqali allaqachon o'qilgan; bo'lmasa shu yerda.
    if (Object.keys(this.currencyService.rates()).length === 0) {
      this.currencyService.loadRates();
    }
  }

  private loadChart(): void {
    this.financeSvc.getIncomeExpense(30).subscribe({
      next: (res) => {
        const data = res.data ?? [];
        if (data.length === 0) return;
        this.chartConfig.set({
          series: [
            { name: this.language.translate('finance.income'), data: data.map((d) => d.income) },
            { name: this.language.translate('finance.expense'), data: data.map((d) => d.expense) },
          ],
          chart: { ...APEX_DEFAULTS.chart, type: 'area', height: 280 },
          xaxis: {
            categories: data.map((d) =>
              new Date(d.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })
            ),
          },
          colors: ['#10b981', '#ef4444'],
          grid: APEX_DEFAULTS.grid,
          tooltip: APEX_DEFAULTS.tooltip,
          fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.1 } },
          stroke: { curve: 'smooth', width: 2 },
          legend: { position: 'top' },
        });
      },
      error: () => undefined,
    });
  }

  private loadDebtorsChart(): void {
    this.financeSvc.getTopDebtors(5).subscribe({
      next: (res) => {
        const data = res.data ?? [];
        if (data.length === 0) return;
        this.debtorsChart.set({
          series: [{ name: this.language.translate('finance.debt'), data: data.map((d) => Math.abs(d.debtAmount)) }],
          chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 },
          xaxis: { categories: data.map((d) => d.counterpartyName) },
          colors: ['#f59e0b'],
          grid: APEX_DEFAULTS.grid,
          tooltip: APEX_DEFAULTS.tooltip,
          plotOptions: { bar: { borderRadius: 4, horizontal: true, columnWidth: '60%' } },
          legend: { show: false },
        });
      },
      error: () => undefined,
    });
  }

  selectCurrency(code: string): void {
    this.selectedCurrency.set(code);
    this.customRate.set(null);
    const f = this.foreignAmount();
    if (f) this.uzsAmount.set(f * this.currentRate());
  }

  onForeignChange(val: number | null): void {
    this.foreignAmount.set(val);
    this.uzsAmount.set(val && this.currentRate() ? val * this.currentRate() : null);
  }

  onUzsChange(val: number | null): void {
    this.uzsAmount.set(val);
    this.foreignAmount.set(val && this.currentRate() ? val / this.currentRate() : null);
  }

  swapAmounts(): void {
    const f = this.foreignAmount();
    const u = this.uzsAmount();
    this.foreignAmount.set(u && this.currentRate() ? u / this.currentRate() : null);
    this.uzsAmount.set(f ? f * this.currentRate() : null);
  }

  useCbuRate(): void {
    this.customRate.set(null);
    const f = this.foreignAmount();
    if (f) this.uzsAmount.set(f * this.currentRate());
  }
}
