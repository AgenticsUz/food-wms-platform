import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { KpiService } from '../../../core/services/kpi.service';
import { ShiftEfficiencyDto, PlanVsActualDto } from '../../../core/models/analytics.model';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';

@Component({
  selector: 'app-kpi-dashboard',
  standalone: true,
  imports: [NgApexchartsModule, TranslocoDirective, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './kpi-dashboard.component.html',
  styleUrl: './kpi-dashboard.component.scss'
})
export default class KpiDashboardComponent implements OnInit {
  private kpiService = inject(KpiService);

  loading = signal(true);
  efficiencyChart = signal<Record<string, unknown> | null>(null);
  planVsActualChart = signal<Record<string, unknown> | null>(null);

  ngOnInit() {
    this.loadEfficiency();
    this.loadPlanVsActual();
  }

  private loadEfficiency() {
    this.kpiService.getEfficiency(7).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const grouped = new Map<string, number[]>();
          for (const item of res.data) {
            if (!grouped.has(item.shiftName)) {
              grouped.set(item.shiftName, []);
            }
            grouped.get(item.shiftName)!.push(item.efficiencyPercent);
          }

          const labels: string[] = [];
          const series: number[] = [];
          for (const [name, values] of grouped) {
            labels.push(name);
            const avg = values.reduce((a, b) => a + b, 0) / values.length;
            series.push(Math.round(avg * 10) / 10);
          }

          this.efficiencyChart.set({
            series,
            chart: { ...APEX_DEFAULTS.chart, type: 'radialBar', height: 280 },
            labels,
            colors: APEX_DEFAULTS.colors,
            plotOptions: {
              radialBar: {
                hollow: { size: '45%' },
                dataLabels: {
                  name: { fontSize: '14px' },
                  value: { fontSize: '24px', fontFamily: 'JetBrains Mono' }
                }
              }
            }
          });
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadPlanVsActual() {
    this.kpiService.getPlanVsActual(7).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          const dates = [...new Set(res.data.map(d => d.date))];
          const labels = dates.map(d =>
            new Date(d).toLocaleDateString('en', { weekday: 'short', month: 'short', day: 'numeric' })
          );
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
}
