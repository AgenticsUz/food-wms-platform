import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { DatePicker } from 'primeng/datepicker';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Transfer, TransferType } from '../../../core/models/transfer.model';
import { transferTypeClass, transferTypeKey } from '../../../shared/utils/transfer-enums';
import { toLocalDateString, parseUtc } from '../../../shared/utils/date.util';

interface MovementRow {
  productName: string;
  unitShortName: string;
  warehouseName: string;
  type: TransferType;
  quantity: number;
  date: Date | null;
  note: string | null;
}

@Component({
  selector: 'app-movements',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TranslocoDirective, TableModule, DatePicker, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './movements.component.html',
  styleUrl: './movements.component.scss'
})
export default class MovementsComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  // helperlar (template'da chaqirish uchun)
  protected transferTypeClass = transferTypeClass;
  protected transferTypeKey = transferTypeKey;

  movements = signal<MovementRow[]>([]);
  loading = signal(true);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  ngOnInit() { this.loadMovements(); }

  loadMovements() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = { status: 2, pageSize: 500 }; // Confirmed only
    if (this.dateFrom()) params['from'] = toLocalDateString(this.dateFrom()!);
    if (this.dateTo()) params['to'] = toLocalDateString(this.dateTo()!);
    this.warehouseService.getMovements(params).subscribe({
      next: (res) => {
        this.movements.set(res.success && res.data ? this.flatten(res.data) : []);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  /** Har transferni items bo'yicha alohida harakat qatorlariga yoyadi. */
  private flatten(transfers: Transfer[]): MovementRow[] {
    const rows: MovementRow[] = [];
    for (const tr of transfers) {
      const warehouseName = this.warehouseFor(tr);
      const date = parseUtc(tr.confirmedAt ?? tr.createdAt);
      for (const item of tr.items ?? []) {
        rows.push({
          productName: item.productName ?? '—',
          unitShortName: item.unitShortName ?? '',
          warehouseName,
          type: tr.type,
          quantity: item.quantity,
          date,
          note: tr.note
        });
      }
    }
    return rows;
  }

  private warehouseFor(tr: Transfer): string {
    switch (tr.type) {
      case TransferType.Outgoing:
        return tr.fromWarehouseName ?? '—';
      case TransferType.Internal:
        return `${tr.fromWarehouseName ?? '—'} → ${tr.toWarehouseName ?? '—'}`;
      default: // Incoming, Return, ProductionOutput
        return tr.toWarehouseName ?? '—';
    }
  }

  onDateChange() { this.loadMovements(); }
}
