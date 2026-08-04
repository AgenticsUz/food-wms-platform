import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TranslocoDirective } from '@jsverse/transloco';
import { UpgradeBannerComponent } from '../../../shared/components/upgrade-banner/upgrade-banner.component';
import { AgentPortalService } from '../../../core/services/agent-portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { AgentSalesReport, CommissionRecord, CommissionStatus } from '../../../core/models/agent.model';

@Component({
  selector: 'app-agent-portal-dashboard',
  standalone: true,
  imports: [UpgradeBannerComponent, DecimalPipe, DatePipe, TableModule, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './agent-portal-dashboard.component.html',
  styleUrl: './agent-portal-dashboard.component.scss'
})
export default class AgentPortalDashboardComponent implements OnInit {
  private portalService = inject(AgentPortalService);

  report = signal<AgentSalesReport | null>(null);
  commissions = signal<CommissionRecord[]>([]);
  loading = signal(true);

  get agentName(): string {
    return this.portalService.agent()?.name ?? 'Guest';
  }

  ngOnInit() {
    this.portalService.getSales().subscribe({
      next: (res) => {
        if (res.success && res.data) this.report.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    this.portalService.getCommissions().subscribe({
      next: (res) => { if (res.success && res.data) this.commissions.set(res.data); }
    });
  }

  statusLabel(status: CommissionStatus): string {
    switch (status) {
      case CommissionStatus.Confirmed: return 'Confirmed';
      case CommissionStatus.Cancelled: return 'Cancelled';
      default: return 'Pending';
    }
  }
}
