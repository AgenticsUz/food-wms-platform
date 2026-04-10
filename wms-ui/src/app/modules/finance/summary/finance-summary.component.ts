import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { FinanceService } from '../../../core/services/finance.service';
import { FinanceSummary } from '../../../core/models/finance.model';
import { IncomeExpenseDto } from '../../../core/models/analytics.model';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';

@Component({
  selector: 'app-finance-summary',
  standalone: true,
  imports: [DecimalPipe, NgApexchartsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './finance-summary.component.html',
  styleUrl: './finance-summary.component.scss'
})
export default class FinanceSummaryComponent implements OnInit {
  private financeSvc = inject(FinanceService);

  summary = signal<FinanceSummary | null>(null);
  loading = signal(true);
  chartConfig = signal<Record<string, unknown> | null>(null);

  ngOnInit() {
    this.loadSummary();
    this.loadChart();
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
}
