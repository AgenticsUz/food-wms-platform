import type {
  ApexAxisChartSeries,
  ApexChart,
  ApexGrid,
  ApexLegend,
  ApexMarkers,
  ApexNonAxisChartSeries,
  ApexPlotOptions,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
} from 'ng-apexcharts';

/**
 * `<apx-chart>` ga uzatiladigan sozlama — eski ekranlardagi
 * `Record<string, unknown>` + shablondagi `$any(...)` o'rniga (lint `$any` ni
 * taqiqlaydi; turlangan sozlama xatoni kompilyatsiyada ushlaydi).
 */
export interface ChartConfig {
  readonly series: ApexNonAxisChartSeries;
  readonly chart: ApexChart;
  readonly xaxis?: ApexXAxis;
  readonly yaxis?: ApexYAxis;
  readonly colors?: string[];
  readonly grid?: ApexGrid;
  readonly tooltip?: ApexTooltip;
  readonly stroke?: ApexStroke;
  readonly markers?: ApexMarkers;
  readonly legend?: ApexLegend;
  readonly plotOptions?: ApexPlotOptions;
  readonly labels?: string[];
}

/**
 * O'qli grafik seriyasi (`[{ name, data }]`).
 *
 * ⚠️ `ng-apexcharts` 3.0.0 `series` kirishini faqat `ApexNonAxisChartSeries`
 * (`number[]`) deb e'lon qilgan, ApexCharts esa ikkala shaklni ham qabul
 * qiladi. Tur kutubxonada tor — o'tkazish shu BITTA joyda, kutubxona
 * tuzatilganda shu funksiya olib tashlanadi.
 */
export function axisSeries(series: ApexAxisChartSeries): ApexNonAxisChartSeries {
  return series as unknown as ApexNonAxisChartSeries;
}
