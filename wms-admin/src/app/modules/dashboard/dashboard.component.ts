import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { PlatformService } from '../../core/services/platform.service';
import { PlatformStats } from '../../core/models/plan.model';
import { ExpiringTenant } from '../../core/models/tenant.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [TranslocoDirective, DatePipe, DecimalPipe, RouterLink, NgApexchartsModule, TableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export default class DashboardComponent implements OnInit {
  private service = inject(PlatformService);
  private transloco = inject(TranslocoService);

  stats = signal<PlatformStats | null>(null);
  loading = signal(true);
  chart = signal<Record<string, unknown> | null>(null);

  /**
   * Serverdan tayyor ro'yxat: `daysLeft` manfiy bo'lsa muddat o'tgan.
   * Faqat `paid` turi hisoblanadi — kartani bosganda ochiladigan jadval
   * filtri ham to'lov muddati bo'yicha ishlaydi, raqamlar mos kelsin.
   */
  private expiringTenants = signal<ExpiringTenant[]>([]);

  expiringSoon = computed(() =>
    this.expiringTenants().filter(t => t.kind === 'paid' && t.daysLeft >= 0).length);

  expired = computed(() =>
    this.expiringTenants().filter(t => t.kind === 'paid' && t.daysLeft < 0).length);

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
    this.service.getExpiring(7).subscribe(res => {
      if (res.success && res.data) this.expiringTenants.set(res.data);
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
    const key = status === 2 ? 'Active' : status === 1 ? 'Trial' : status === 3 ? 'Suspended' : null;
    return key ? this.transloco.translate('status.' + key) : '—';
  }
  statusClass(status: number): string {
    return status === 2 ? 'pill pill-success' : status === 1 ? 'pill pill-warning' : status === 3 ? 'pill pill-danger' : 'pill pill-neutral';
  }
}
