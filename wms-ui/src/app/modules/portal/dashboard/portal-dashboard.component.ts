import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { UpgradeBannerComponent } from '../../../shared/components/upgrade-banner/upgrade-banner.component';
import { PortalService } from '../../../core/services/portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PortalFinance } from '../../../core/models/settings.model';
import { Transfer } from '../../../core/models/transfer.model';
import { transferStatusClass, transferStatusKey, transferTypeKey } from '../../../shared/utils/transfer-enums';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  imports: [UpgradeBannerComponent, DecimalPipe, DatePipe, RouterLink, TableModule, Button, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-dashboard.component.html',
  styleUrl: './portal-dashboard.component.scss'
})
export default class PortalDashboardComponent implements OnInit {
  private portalService = inject(PortalService);

  // helperlar (template'da chaqirish uchun)
  protected transferStatusClass = transferStatusClass;
  protected transferStatusKey = transferStatusKey;
  protected transferTypeKey = transferTypeKey;

  finance = signal<PortalFinance | null>(null);
  transfers = signal<Transfer[]>([]);
  loading = signal(true);

  get counterpartyName(): string {
    return this.portalService.counterparty()?.name ?? '';
  }

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.portalService.getFinance().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.finance.set(res.data);
        }
      }
    });

    this.portalService.getTransfers({ page: 1, pageSize: 5 }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.transfers.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
