import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { TranslocoDirective } from '@jsverse/transloco';
import { AnalyticsService } from '../../core/services/analytics.service';
import { AuthService } from '../../core/services/auth.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { APEX_DEFAULTS } from '../../core/config/apex-defaults';
import {
  DashboardSummaryDto,
  PlanVsActualDto,
  DailyTransferDto,
  ProductDistributionDto,
  RecentTransferDto
} from '../../core/models/analytics.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DecimalPipe, DatePipe, NgApexchartsModule, TableModule, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export default class DashboardComponent implements OnInit {
  private analyticsService = inject(AnalyticsService);
  private authService = inject(AuthService);

  userName = this.authService.currentUser()?.fullName ?? 'User';
  today = new Date();

  loading = signal(true);
  summary = signal<DashboardSummaryDto | null>(null);
  recentTransfers = signal<RecentTransferDto[]>([]);

  // Chart configs
  planVsActualChart = signal<Record<string, unknown> | null>(null);
  dailyTransfersChart = signal<Record<string, unknown> | null>(null);
  productDistributionChart = signal<Record<string, unknown> | null>(null);

  ngOnInit() {
    this.loadSummary();
    this.loadPlanVsActual();
    this.loadDailyTransfers();
    this.loadProductDistribution();
    this.loadRecentTransfers();
  }

  private loadSummary() {
    this.analyticsService.getSummary().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summary.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadPlanVsActual() {
    this.analyticsService.getProductionPlanVsActual(7).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const dates = [...new Set(res.data.map(d => d.date))];
          const labels = dates.map(d => new Date(d).toLocaleDateString('en', { weekday: 'short', month: 'short', day: 'numeric' }));
          const planned = dates.map(date =>
            res.data!.filter(d => d.date === date).reduce((sum, d) => sum + d.planned, 0)
          );
          const actual = dates.map(date =>
            res.data!.filter(d => d.date === date).reduce((sum, d) => sum + d.actual, 0)
          );

          this.planVsActualChart.set({
            series: [
              { name: 'Planned', data: planned },
              { name: 'Actual', data: actual }
            ],
            chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 280 },
            colors: APEX_DEFAULTS.colors,
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            xaxis: { categories: labels },
            stroke: { curve: 'smooth', width: 3 },
            markers: { size: 4 },
            legend: { position: 'top' }
          });
        }
      }
    });
  }

  private loadDailyTransfers() {
    this.analyticsService.getDailyTransfers(7).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const labels = res.data.map(d =>
            new Date(d.date).toLocaleDateString('en', { weekday: 'short', day: 'numeric' })
          );
          this.dailyTransfersChart.set({
            series: [
              { name: 'Incoming', data: res.data.map(d => d.incomingTotal) },
              { name: 'Outgoing', data: res.data.map(d => d.outgoingTotal) }
            ],
            chart: { ...APEX_DEFAULTS.chart, type: 'bar', height: 240 },
            colors: ['#6366f1', '#f59e0b'],
            grid: APEX_DEFAULTS.grid,
            tooltip: APEX_DEFAULTS.tooltip,
            xaxis: { categories: labels },
            plotOptions: { bar: { borderRadius: 6, columnWidth: '55%' } },
            legend: { position: 'top' }
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
            labels: res.data.map(d => d.productType),
            colors: APEX_DEFAULTS.colors,
            tooltip: APEX_DEFAULTS.tooltip,
            legend: { position: 'bottom' },
            plotOptions: {
              pie: {
                donut: { size: '60%', labels: { show: true, total: { show: true, label: 'Total' } } }
              }
            }
          });
        }
      }
    });
  }

  private loadRecentTransfers() {
    this.analyticsService.getRecentTransfers(5).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.recentTransfers.set(res.data);
        }
      }
    });
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }

  getTransferStatusLabel(status: string): string {
    const map: Record<string, string> = {
      Pending: 'Pending',
      Confirmed: 'Confirmed',
      Rejected: 'Rejected',
      Cancelled: 'Cancelled'
    };
    return map[status] ?? status;
  }
}
