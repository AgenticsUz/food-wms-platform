import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { TranslocoDirective } from '@jsverse/transloco';
import { AnalyticsService } from '../../core/services/analytics.service';
import { ExportService } from '../../core/services/export.service';
import { AuthService } from '../../core/services/auth.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { APEX_DEFAULTS } from '../../core/config/apex-defaults';
import {
  DashboardSummaryDto,
  RecentTransferDto,
  ExtendedDashboardSummaryDto,
  MonthlyComparisonDto
} from '../../core/models/analytics.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, NgApexchartsModule, TableModule, Button, DatePicker, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export default class DashboardComponent implements OnInit {
  private analyticsService = inject(AnalyticsService);
  private authService = inject(AuthService);
  private router = inject(Router);
  exportService = inject(ExportService);

  userName = this.authService.currentUser()?.fullName ?? 'User';
  today = new Date();

  loading = signal(true);
  summary = signal<DashboardSummaryDto | null>(null);
  extSummary = signal<ExtendedDashboardSummaryDto | null>(null);
  extLoading = signal(true);
  recentTransfers = signal<RecentTransferDto[]>([]);
  selectedDays = signal(7);

  // Date range
  fromDate = signal<Date | null>(null);
  toDate = signal<Date | null>(null);

  // Chart configs
  planVsActualChart = signal<Record<string, unknown> | null>(null);
  dailyTransfersChart = signal<Record<string, unknown> | null>(null);
  productDistributionChart = signal<Record<string, unknown> | null>(null);
  monthlyChart = signal<Record<string, unknown> | null>(null);
  monthlyLoading = signal(true);

  ngOnInit() {
    this.loadSummary();
    this.loadExtendedSummary();
    this.loadAllCharts();
    this.loadRecentTransfers();
    this.loadMonthlyComparison();
  }

  // --- Quick range buttons ---
  changeDays(days: number) {
    this.selectedDays.set(days);
    this.fromDate.set(null);
    this.toDate.set(null);
    this.planVsActualChart.set(null);
    this.dailyTransfersChart.set(null);
    this.loadAllCharts();
  }

  setQuickRange(range: string) {
    const now = new Date();
    let from: Date;
    let to: Date = new Date(now);

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
      default:
        return;
    }

    this.fromDate.set(from);
    this.toDate.set(to);
    this.applyDateRange();
  }

  applyDateRange() {
    this.loadExtendedSummary();
  }

  resetDateRange() {
    this.fromDate.set(null);
    this.toDate.set(null);
    this.loadExtendedSummary();
  }

  // --- Data loaders ---
  private loadSummary() {
    this.analyticsService.getSummary().subscribe({
      next: (res) => {
        if (res.success && res.data) this.summary.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadExtendedSummary() {
    this.extLoading.set(true);
    const from = this.fromDate() ? this.formatDate(this.fromDate()!) : undefined;
    const to = this.toDate() ? this.formatDate(this.toDate()!) : undefined;
    this.analyticsService.getExtendedSummary(from, to).subscribe({
      next: (res) => {
        if (res.success && res.data) this.extSummary.set(res.data);
        this.extLoading.set(false);
      },
      error: () => this.extLoading.set(false)
    });
  }

  private loadAllCharts() {
    this.loadPlanVsActual();
    this.loadDailyTransfers();
    this.loadProductDistribution();
  }

  private loadPlanVsActual() {
    this.analyticsService.getProductionPlanVsActual(this.selectedDays()).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const dates = [...new Set(res.data.map(d => d.date))];
          const labels = dates.map(d => new Date(d).toLocaleDateString('en', { weekday: 'short', month: 'short', day: 'numeric' }));
          const planned = dates.map(date => res.data!.filter(d => d.date === date).reduce((sum, d) => sum + d.planned, 0));
          const actual = dates.map(date => res.data!.filter(d => d.date === date).reduce((sum, d) => sum + d.actual, 0));
          this.planVsActualChart.set({
            series: [{ name: 'Planned', data: planned }, { name: 'Actual', data: actual }],
            chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 280 },
            colors: APEX_DEFAULTS.colors, grid: APEX_DEFAULTS.grid, tooltip: APEX_DEFAULTS.tooltip,
            xaxis: { categories: labels }, stroke: { curve: 'smooth', width: 3 }, markers: { size: 4 }, legend: { position: 'top' }
          });
        }
      }
    });
  }

  private loadDailyTransfers() {
    this.analyticsService.getDailyTransfers(this.selectedDays()).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const labels = res.data.map(d => new Date(d.date).toLocaleDateString('en', { weekday: 'short', day: 'numeric' }));
          this.dailyTransfersChart.set({
            series: [{ name: 'Incoming', data: res.data.map(d => d.incomingTotal) }, { name: 'Outgoing', data: res.data.map(d => d.outgoingTotal) }],
            chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 }, colors: ['#6366f1', '#f59e0b'],
            grid: APEX_DEFAULTS.grid, tooltip: APEX_DEFAULTS.tooltip, xaxis: { categories: labels },
            plotOptions: { bar: { borderRadius: 4, columnWidth: '60%' } }, legend: { position: 'top' }
          });
        }
      }
    });
  }

  private loadProductDistribution() {
    this.analyticsService.getProductDistribution().subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.productDistributionChart.set({
            series: res.data.map(d => d.count),
            chart: { ...APEX_DEFAULTS.chart, type: 'donut', height: 240 },
            labels: res.data.map(d => d.productType), colors: APEX_DEFAULTS.colors, tooltip: APEX_DEFAULTS.tooltip,
            legend: { position: 'bottom' },
            plotOptions: { pie: { donut: { size: '65%', labels: { show: true, total: { show: true, label: 'Total' } } } } }
          });
        }
      }
    });
  }

  private loadMonthlyComparison() {
    this.monthlyLoading.set(true);
    this.analyticsService.getMonthlyComparison().subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.monthlyChart.set({
            series: [
              { name: 'Income', type: 'column', data: res.data.map(d => d.income) },
              { name: 'Expense', type: 'column', data: res.data.map(d => d.expense) },
              { name: 'Net', type: 'line', data: res.data.map(d => d.net) }
            ],
            chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 300, stacked: false },
            colors: ['#10b981', '#ef4444', '#6366f1'],
            grid: APEX_DEFAULTS.grid, tooltip: APEX_DEFAULTS.tooltip,
            xaxis: { categories: res.data.map(d => d.month) },
            stroke: { width: [0, 0, 3], curve: 'smooth' },
            plotOptions: { bar: { borderRadius: 4, columnWidth: '50%' } },
            legend: { position: 'top' },
            yaxis: { labels: { formatter: (val: number) => val >= 1000 ? (val / 1000).toFixed(0) + 'k' : val.toString() } }
          });
        }
        this.monthlyLoading.set(false);
      },
      error: () => this.monthlyLoading.set(false)
    });
  }

  private loadRecentTransfers() {
    this.analyticsService.getRecentTransfers(5).subscribe({
      next: (res) => {
        if (res.success && res.data) this.recentTransfers.set(res.data);
      }
    });
  }

  // --- Export helpers ---
  exportReport(type: string) {
    const from = this.fromDate() ? this.formatDate(this.fromDate()!) : undefined;
    const to = this.toDate() ? this.formatDate(this.toDate()!) : undefined;
    const today = this.formatDate(new Date());
    const params: Record<string, unknown> = {};
    if (from) params['fromDate'] = from;
    if (to) params['toDate'] = to;

    switch (type) {
      case 'finance': this.exportService.download('export/transactions', `finance-${today}.xlsx`, params); break;
      case 'stock': this.exportService.download('export/stock', `stock-${today}.xlsx`); break;
      case 'transfers': this.exportService.download('export/transfers', `transfers-${today}.xlsx`, params); break;
      case 'debtors': this.exportService.download('export/counterparties', `debtors-${today}.xlsx`); break;
    }
  }

  // --- Helpers ---
  navigateToTransfer(id: number) { this.router.navigate(['/transfers', id]); }

  getGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'morning';
    if (hour < 18) return 'afternoon';
    return 'evening';
  }

  getTransferStatusLabel(status: string): string {
    return status;
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }
}
