import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { PlatformService } from '../../core/services/platform.service';
import { PlatformStats } from '../../core/models/plan.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe, NgApexchartsModule, TableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export default class DashboardComponent implements OnInit {
  private service = inject(PlatformService);

  stats = signal<PlatformStats | null>(null);
  loading = signal(true);
  chart = signal<Record<string, unknown> | null>(null);

  ngOnInit() {
    this.service.getStats().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.stats.set(res.data);
          this.buildChart(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private buildChart(s: PlatformStats) {
    if (!s.monthlyGrowth?.length) return;
    this.chart.set({
      series: [{ name: 'New tenants', data: s.monthlyGrowth.map(m => m.count) }],
      chart: { type: 'area', height: 260, toolbar: { show: false }, fontFamily: 'inherit' },
      colors: ['#5e9540'],
      dataLabels: { enabled: false },
      stroke: { curve: 'smooth', width: 3 },
      fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0.05 } },
      xaxis: { categories: s.monthlyGrowth.map(m => m.month) },
      grid: { borderColor: '#e6e0d6', strokeDashArray: 4 }
    });
  }

  statusLabel(status: number): string {
    return status === 2 ? 'Active' : status === 1 ? 'Trial' : status === 3 ? 'Suspended' : '—';
  }
  statusClass(status: number): string {
    return status === 2 ? 'pill pill-success' : status === 1 ? 'pill pill-warning' : status === 3 ? 'pill pill-danger' : 'pill pill-neutral';
  }
}
