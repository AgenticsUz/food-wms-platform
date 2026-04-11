import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { FinanceService } from '../../../core/services/finance.service';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { CurrencyService } from '../../../core/services/currency.service';
import { FinanceSummary } from '../../../core/models/finance.model';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';

@Component({
  selector: 'app-finance-summary',
  standalone: true,
  imports: [DecimalPipe, FormsModule, NgApexchartsModule, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './finance-summary.component.html',
  styleUrl: './finance-summary.component.scss'
})
export default class FinanceSummaryComponent implements OnInit {
  private financeSvc = inject(FinanceService);
  private analyticsService = inject(AnalyticsService);
  currencyService = inject(CurrencyService);

  summary = signal<FinanceSummary | null>(null);
  loading = signal(true);
  chartConfig = signal<Record<string, unknown> | null>(null);
  debtorsChart = signal<Record<string, unknown> | null>(null);

  // Currency calculator
  selectedCurrency = signal('USD');
  foreignAmount = signal<number | null>(null);
  uzsAmount = signal<number | null>(null);
  customRate = signal<number | null>(null);

  currentRate = computed(() => {
    return this.customRate() || this.currencyService.rates()[this.selectedCurrency()] || 0;
  });

  lastUpdatedFormatted = computed(() => {
    const d = this.currencyService.lastUpdated();
    if (!d) return '';
    return new Date(d).toLocaleDateString('uz-UZ', {
      day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit'
    });
  });

  ngOnInit() {
    this.loadSummary();
    this.loadChart();
    this.loadDebtorsChart();
  }

  private loadSummary() {
    this.financeSvc.getSummary().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summary.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadChart() {
    this.financeSvc.getIncomeExpense(30).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const labels = res.data.map(d =>
            new Date(d.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })
          );
          const incomes = res.data.map(d => d.income);
          const expenses = res.data.map(d => d.expense);

          this.chartConfig.set({
            series: [
              { name: 'Income', data: incomes },
              { name: 'Expense', data: expenses }
            ],
            chart: { ...APEX_DEFAULTS.chart, type: 'area', height: 280 },
            xaxis: { categories: labels },
            colors: ['#10b981', '#ef4444'],
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            fill: {
              type: 'gradient',
              gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.1 }
            },
            stroke: { curve: 'smooth', width: 2 },
            legend: { position: 'top' }
          });
        }
      }
    });
  }

  selectCurrency(code: string) {
    this.selectedCurrency.set(code);
    this.customRate.set(null);
    const f = this.foreignAmount();
    if (f) this.uzsAmount.set(f * this.currentRate());
  }

  onForeignChange(val: number | null) {
    this.foreignAmount.set(val);
    this.uzsAmount.set(val && this.currentRate() ? val * this.currentRate() : null);
  }

  onUzsChange(val: number | null) {
    this.uzsAmount.set(val);
    this.foreignAmount.set(val && this.currentRate() ? val / this.currentRate() : null);
  }

  swapAmounts() {
    const f = this.foreignAmount();
    const u = this.uzsAmount();
    this.foreignAmount.set(u && this.currentRate() ? u / this.currentRate() : null);
    this.uzsAmount.set(f ? f * this.currentRate() : null);
  }

  useCbuRate() {
    this.customRate.set(null);
    const f = this.foreignAmount();
    if (f) this.uzsAmount.set(f * this.currentRate());
  }

  private loadDebtorsChart() {
    this.analyticsService.getTopDebtors(5).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.debtorsChart.set({
            series: [{ name: 'Debt', data: res.data.map(d => Math.abs(d.debtAmount)) }],
            chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 },
            xaxis: { categories: res.data.map(d => d.counterpartyName) },
            colors: ['#f59e0b'],
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            plotOptions: { bar: { borderRadius: 4, horizontal: true, columnWidth: '60%' } },
            legend: { show: false }
          });
        }
      }
    });
  }
}
