import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgApexchartsModule } from 'ng-apexcharts';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Select } from 'primeng/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { AgentService } from '../../../core/services/agent.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { AgentSalesReport, CommissionRecord, CommissionStatus, PayCommissionDto } from '../../../core/models/agent.model';
import { APEX_DEFAULTS } from '../../../core/config/apex-defaults';

@Component({
  selector: 'app-agent-detail',
  standalone: true,
  imports: [
    DecimalPipe, DatePipe, FormsModule, RouterLink, NgApexchartsModule,
    TableModule, Button, InputText, InputNumber, Dialog, ToggleSwitch, Select,
    PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective, TranslocoDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './agent-detail.component.html',
  styleUrl: './agent-detail.component.scss'
})
export default class AgentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private agentService = inject(AgentService);
  private notify = inject(NotificationService);

  agentId = signal<number>(0);
  report = signal<AgentSalesReport | null>(null);
  commissions = signal<CommissionRecord[]>([]);
  loading = signal(true);
  chartConfig = signal<Record<string, unknown> | null>(null);

  payDialogVisible = signal(false);
  paying = signal(false);
  payForm = signal<PayCommissionDto>({ amount: 0, note: null, recordAsExpense: true });

  statusOptionsFor(t: (key: string) => string) {
    return [
      { label: t('agent.statusPending'), value: CommissionStatus.Pending },
      { label: t('agent.statusConfirmed'), value: CommissionStatus.Confirmed },
      { label: t('agent.statusCancelled'), value: CommissionStatus.Cancelled }
    ];
  }

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.agentId.set(id);
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    const id = this.agentId();
    this.agentService.getSales(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.report.set(res.data);
          this.buildChart(res.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load agent report');
      }
    });
    this.agentService.getCommissions(id).subscribe({
      next: (res) => this.commissions.set(res.success && res.data ? res.data : []),
      error: () => this.notify.error('Failed to load commissions')
    });
  }

  private buildChart(report: AgentSalesReport) {
    if (!report.timeline || report.timeline.length === 0) {
      this.chartConfig.set(null);
      return;
    }
    const labels = report.timeline.map(p =>
      new Date(p.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })
    );
    this.chartConfig.set({
      series: [
        { name: 'Sales', data: report.timeline.map(p => p.saleAmount) },
        { name: 'Commission', data: report.timeline.map(p => p.commissionAmount) }
      ],
      chart: { ...APEX_DEFAULTS.chart, type: 'line', height: 280 },
      xaxis: { categories: labels },
      colors: ['#6366f1', '#10b981'],
      grid: APEX_DEFAULTS.grid,
      tooltip: APEX_DEFAULTS.tooltip,
      stroke: { curve: 'smooth', width: 2 },
      legend: { position: 'top' }
    });
  }

  statusLabel(status: CommissionStatus): string {
    switch (status) {
      case CommissionStatus.Confirmed: return 'Confirmed';
      case CommissionStatus.Cancelled: return 'Cancelled';
      default: return 'Pending';
    }
  }

  changeStatus(record: CommissionRecord, status: CommissionStatus) {
    if (record.status === status) return;
    this.agentService.updateCommissionStatus(record.id, status).subscribe({
      next: () => {
        this.notify.success('Status updated');
        this.loadData();
      },
      error: () => this.notify.error('Failed to update status')
    });
  }

  openPay() {
    this.payForm.set({ amount: this.report()?.commissionDue ?? 0, note: null, recordAsExpense: true });
    this.payDialogVisible.set(true);
  }

  pay() {
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
      error: () => {
        this.paying.set(false);
        this.notify.error('Failed to pay commission');
      }
    });
  }

  updatePayForm(field: string, value: unknown) {
    this.payForm.update(f => ({ ...f, [field]: value }));
  }
}
