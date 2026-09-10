import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import {
  ChartComponent,
  type ApexAxisChartSeries,
  type ApexChart,
  type ApexGrid,
  type ApexLegend,
  type ApexMarkers,
  type ApexNonAxisChartSeries,
  type ApexPlotOptions,
  type ApexStroke,
  type ApexTooltip,
  type ApexXAxis,
} from 'ng-apexcharts';

import { WmsSession } from '../../../core/auth/wms-session';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';
import { parseUtc } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { PlanVsActualDto, ShiftEfficiencyDto } from '../kpi.model';
import { KpiService } from '../kpi.service';

/** Grafik sozlamasi TIPLI — eski shablondagi 14 ta `$any()` o'rniga. */
interface EfficiencyChart {
  readonly series: ApexNonAxisChartSeries;
  readonly chart: ApexChart;
  readonly labels: string[];
  readonly colors: string[];
  readonly plotOptions: ApexPlotOptions;
}

interface PlanVsActualChart {
  readonly series: ApexAxisChartSeries;
  readonly chart: ApexChart;
  readonly xaxis: ApexXAxis;
  readonly colors: string[];
  readonly grid: ApexGrid;
  readonly tooltip: ApexTooltip;
  readonly stroke: ApexStroke;
  readonly markers: ApexMarkers;
  readonly legend: ApexLegend;
}

@Component({
  selector: 'app-kpi-dashboard',
  imports: [ChartComponent, TranslocoDirective, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './kpi-dashboard.component.html',
  styleUrl: './kpi-dashboard.component.scss',
})
export default class KpiDashboardComponent implements OnInit {
  private readonly kpiService = inject(KpiService);
  private readonly session = inject(WmsSession);
  private readonly language = inject(LanguageService);

  readonly loading = signal(true);
  readonly efficiencyChart = signal<EfficiencyChart | null>(null);
  readonly planVsActualChart = signal<PlanVsActualChart | null>(null);

  ngOnInit(): void {
    // Ikkala grafik `analytics/*` dan: backend `dashboard.view` ruxsati va
    // `analytics.advanced` feature'ini talab qiladi (KPI ruxsati emas). Shart
    // bajarilmasa so'rov yuborilmaydi — aks holda KPI sahifasini ochgan har
    // odam 403 toastini ko'rardi; grafik o'rnida «ma'lumot yo'q» qoladi.
    const analytics =
      this.session.can('dashboard.view') && this.session.isFeatureEnabled('analytics.advanced');
    if (!analytics) {
      this.loading.set(false);
      return;
    }
    this.loadEfficiency();
    if (this.session.isModuleEnabled('PRODUCTION')) {
      this.loadPlanVsActual();
    }
  }

  private loadEfficiency(): void {
    this.kpiService.getEfficiency(7).subscribe({
      next: (res) => {
        const data = res.success ? (res.data ?? []) : [];
        if (data.length > 0) {
          this.efficiencyChart.set(buildEfficiencyChart(data));
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private loadPlanVsActual(): void {
    this.kpiService.getPlanVsActual(7).subscribe({
      next: (res) => {
        const data = res.success ? (res.data ?? []) : [];
        if (data.length === 0) {
          return;
        }
        this.planVsActualChart.set(
          buildPlanVsActualChart(data, this.language.locale(), {
            planned: this.language.translate('kpi.planned'),
            actual: this.language.translate('kpi.actual'),
          })
        );
      },
    });
  }
}

/** Smena bo'yicha o'rtacha samaradorlik (7 kun). */
function buildEfficiencyChart(data: readonly ShiftEfficiencyDto[]): EfficiencyChart {
  const grouped = new Map<string, number[]>();
  for (const item of data) {
    const values = grouped.get(item.shiftName) ?? [];
    values.push(item.efficiencyPercent);
    grouped.set(item.shiftName, values);
  }

  const labels: string[] = [];
  const series: number[] = [];
  for (const [name, values] of grouped) {
    labels.push(name);
    const avg = values.reduce((a, b) => a + b, 0) / values.length;
    series.push(Math.round(avg * 10) / 10);
  }

  return {
    series,
    chart: { ...APEX_DEFAULTS.chart, type: 'radialBar', height: 280 },
    labels,
    colors: APEX_DEFAULTS.colors,
    plotOptions: {
      radialBar: {
        hollow: { size: '60%' },
        dataLabels: {
          name: { fontSize: '14px' },
          value: { fontSize: '24px', fontFamily: 'JetBrains Mono' },
        },
      },
    },
  };
}

/**
 * Kun bo'yicha reja va haqiqiy yig'indisi. Sana yorlig'i foydalanuvchi tilida
 * (eskisida `'en'` qotirilgan edi) va UTC'dan mahalliy kunga o'giriladi.
 */
function buildPlanVsActualChart(
  data: readonly PlanVsActualDto[],
  locale: string,
  names: { planned: string; actual: string }
): PlanVsActualChart {
  const dates = [...new Set(data.map((d) => d.date))];
  const labels = dates.map((d) =>
    (parseUtc(d) ?? new Date(d)).toLocaleDateString(locale, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    })
  );
  const sumBy = (date: string, pick: (d: PlanVsActualDto) => number): number =>
    data.filter((d) => d.date === date).reduce((sum, d) => sum + pick(d), 0);

  return {
    series: [
      { name: names.planned, data: dates.map((date) => sumBy(date, (d) => d.planned)) },
      { name: names.actual, data: dates.map((date) => sumBy(date, (d) => d.actual)) },
    ],
    chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 280 },
    colors: APEX_DEFAULTS.colors,
    grid: APEX_DEFAULTS.grid,
    tooltip: APEX_DEFAULTS.tooltip,
    xaxis: { categories: labels },
    stroke: { curve: 'smooth', width: 3 },
    markers: { size: 4 },
    legend: { position: 'top' },
  };
}
