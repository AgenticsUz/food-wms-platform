import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  ChartComponent,
  type ApexAxisChartSeries,
  type ApexChart,
  type ApexGrid,
  type ApexLegend,
  type ApexStroke,
  type ApexTooltip,
  type ApexXAxis,
} from 'ng-apexcharts';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Select } from 'primeng/select';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PortalAccountCardComponent } from '../../../shared/components/portal-account/portal-account-card.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { shortId } from '../../production/production.model';
import {
  CommissionStatus,
  type Agent,
  type AgentSalesReport,
  type CommissionRecord,
  type PayCommissionDto,
} from '../agent.model';
import { AgentService } from '../agent.service';
import { parseUtc } from '../../../core/utils/date.util';

interface LineChart {
  readonly series: ApexAxisChartSeries;
  readonly chart: ApexChart;
  readonly xaxis: ApexXAxis;
  readonly colors: string[];
  readonly grid: ApexGrid;
  readonly tooltip: ApexTooltip;
  readonly stroke: ApexStroke;
  readonly legend: ApexLegend;
}

/** Holat → CSS kaliti va `agent.status*` tarjima kaliti qo'shimchasi (eskisidagi bilan bir xil). */
function statusLabel(status: CommissionStatus): 'Pending' | 'Confirmed' | 'Cancelled' {
  switch (status) {
    case CommissionStatus.Confirmed:
      return 'Confirmed';
    case CommissionStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Pending';
  }
}

@Component({
  selector: 'app-agent-detail',
  imports: [
    DecimalPipe, DatePipe, FormsModule, RouterLink, ChartComponent,
    TableModule, Button, InputText, InputNumber, Dialog, ToggleSwitch, Select,
    PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective, TranslocoDirective,
    PortalAccountCardComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './agent-detail.component.html',
  styleUrl: './agent-detail.component.scss',
})
export default class AgentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly agentService = inject(AgentService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  /** Tranzaksiya id'si Guid — jadvalda qisqa ko'rinishi (production.model izohi). */
  protected readonly shortId = shortId;
  protected readonly statusLabel = statusLabel;

  readonly agentId = signal<string>('');
  readonly report = signal<AgentSalesReport | null>(null);
  readonly commissions = signal<CommissionRecord[]>([]);
  readonly loading = signal(true);
  readonly chartConfig = signal<LineChart | null>(null);

  readonly payDialogVisible = signal(false);
  readonly paying = signal(false);
  readonly payForm = signal<PayCommissionDto>({ amount: 0, note: null, recordAsExpense: true });

  /** Til almashganda yorliqlar qayta hisoblansin. */
  readonly statusOptions = computed(() => {
    this.language.language();
    return [CommissionStatus.Pending, CommissionStatus.Confirmed, CommissionStatus.Cancelled].map((value) => ({
      label: this.language.translate('agent.status' + statusLabel(value)),
      value,
    }));
  });

  /** Agent yozuvi (telefon uchun) — hisobot DTO'sida telefon yo'q. */
  readonly agent = signal<Agent | null>(null);

  ngOnInit(): void {
    this.agentId.set(this.route.snapshot.paramMap.get('id') ?? '');
    this.loadData();
  }

  loadData(): void {
    const id = this.agentId();
    if (!id) return;
    this.loading.set(true);
    this.agentService.getSales(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.report.set(res.data);
          this.buildChart(res.data);
        }
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi (eskisidagi ikkinchi toast olib tashlandi).
      error: () => this.loading.set(false),
    });
    // Kabinet kartochkasiga telefon kerak — hisob AYNAN shu raqam bilan ochiladi.
    this.agentService.getAgent(id).subscribe({
      next: (res) => this.agent.set(res.data ?? null),
      error: () => undefined,
    });
    this.agentService.getCommissions(id).subscribe({
      next: (res) => this.commissions.set(res.data ?? []),
      // `agents.commissions` feature'i yopiq — jadval bo'sh qoladi (servis izohi).
      error: () => undefined,
    });
  }

  private buildChart(report: AgentSalesReport): void {
    if (report.timeline.length === 0) {
      this.chartConfig.set(null);
      return;
    }
    this.chartConfig.set({
      series: [
        { name: this.language.translate('agent.sales'), data: report.timeline.map((p) => p.saleAmount) },
        { name: this.language.translate('agent.commission'), data: report.timeline.map((p) => p.commissionAmount) },
      ],
      chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 280 },
      xaxis: {
        categories: report.timeline.map((p) =>
          new Date(p.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })
        ),
      },
      colors: ['#6366f1', '#10b981'],
      grid: APEX_DEFAULTS.grid,
      tooltip: APEX_DEFAULTS.tooltip,
      stroke: { curve: 'smooth', width: 2 },
      legend: { position: 'top' },
    });
  }

  changeStatus(record: CommissionRecord, status: CommissionStatus): void {
    if (record.status === status) return;
    this.agentService.updateCommissionStatus(record.id, status).subscribe({
      next: () => {
        this.notify.success('Status updated');
        this.loadData();
      },
      // Xato toastini qobiq chiqaradi; tanlov server holatiga qaytishi uchun qayta o'qiladi.
      error: () => this.loadData(),
    });
  }

  openPay(): void {
    this.payForm.set({ amount: this.report()?.commissionDue ?? 0, note: null, recordAsExpense: true });
    this.payDialogVisible.set(true);
  }

  pay(): void {
    const f = this.payForm();
    if (!f.amount || f.amount <= 0) {
      this.notify.warn('Amount must be greater than 0');
      return;
    }
    this.paying.set(true);
    this.agentService.payCommission(this.agentId(), f).subscribe({
      next: () => {
        this.paying.set(false);
        this.payDialogVisible.set(false);
        this.notify.success('Commission paid');
        this.loadData();
      },
      // Backend "butun yozuvni qoplamaydigan summa" kabi aniq xabar qaytaradi —
      // uni qobiq ko'rsatadi, bu yerda generic toast qo'ymaymiz.
      error: () => this.paying.set(false),
    });
  }

  updatePayForm<K extends keyof PayCommissionDto>(field: K, value: PayCommissionDto[K]): void {
    this.payForm.update((f) => ({ ...f, [field]: value }));
  }

  /**
   * Serverdagi vaqt UTC'da keladi va `Z` siz kelishi mumkin — `new Date` uni
   * mahalliy deb o'qib 5 soat siljitardi (`core/utils/date.util`).
   */
  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }

}
