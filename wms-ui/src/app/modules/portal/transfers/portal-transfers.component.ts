import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { toLocalDateString } from '../../../shared/utils/date.util';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { TranslocoDirective } from '@jsverse/transloco';
import { PortalService } from '../../../core/services/portal.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { Transfer } from '../../../core/models/transfer.model';
import { transferStatusClass, transferStatusKey, transferTypeKey } from '../../../shared/utils/transfer-enums';

@Component({
  selector: 'app-portal-transfers',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, FormsModule, TableModule, Button, DatePicker, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-transfers.component.html',
  styleUrl: './portal-transfers.component.scss'
})
export default class PortalTransfersComponent implements OnInit {
  private portalService = inject(PortalService);

  // helperlar (template'da chaqirish uchun)
  protected transferStatusClass = transferStatusClass;
  protected transferStatusKey = transferStatusKey;
  protected transferTypeKey = transferTypeKey;

  transfers = signal<Transfer[]>([]);
  loading = signal(true);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  ngOnInit() {
    this.loadTransfers();
  }

  loadTransfers() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = {};
    if (this.dateFrom()) {
      params['from'] = toLocalDateString(this.dateFrom()!);
    }
    if (this.dateTo()) {
      params['to'] = toLocalDateString(this.dateTo()!);
    }

    this.portalService.getTransfers(params).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.transfers.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onDateChange() {
    this.loadTransfers();
  }
}
