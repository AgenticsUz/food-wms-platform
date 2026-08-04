import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { PlatformService } from '../../core/services/platform.service';
import { PlatformStats } from '../../core/models/plan.model';
import { Tenant } from '../../core/models/tenant.model';
import { daysUntil } from '../../core/utils/date.util';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink, NgApexchartsModule, TableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export default class DashboardComponent implements OnInit {
  private service = inject(PlatformService);

  stats = signal<PlatformStats | null>(null);
  loading = signal(true);
  chart = signal<Record<string, unknown> | null>(null);
  private tenants = signal<Tenant[]>([]);

  /** 7 kun ichida to'lov muddati tugaydiganlar. */
  expiringSoon = computed(() => this.tenants().filter(t => {
    const d = daysUntil(t.paidUntil);
    return d !== null && d >= 0 && d <= 7;
  }).length);

  /** Muddati allaqachon o'tganlar. */
  expired = computed(() => this.tenants().filter(t => {
    const d = daysUntil(t.paidUntil);
    return d !== null && d < 0;
  }).length);

  /** Javob berilmagan demo so'rovlari. */
  newLeads = signal(0);

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
    // To'lov muddati kartalari tenant ro'yxatidan hisoblanadi — platforma
    // miqyosida ro'yxat kichik, alohida endpoint kerak emas.
    this.service.getTenants().subscribe(res => {
      if (res.success && res.data) this.tenants.set(res.data);
    });
    this.service.getLeads().subscribe(res => {
      const data = res.success ? res.data : null;
      const list = Array.isArray(data) ? data : (data?.items ?? []);
      this.newLeads.set(list.filter(l => l.status === 'New').length);
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
