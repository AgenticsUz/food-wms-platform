import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { DatePicker } from 'primeng/datepicker';

import { localDayRangeToUtc, parseUtc } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { TransferStatus, TransferType, type Transfer } from '../../transfers/transfer.model';
import { transferTypeClass, transferTypeKey } from '../../transfers/transfer-enums';
import { WarehouseService } from '../warehouse.service';

interface MovementRow {
  readonly productName: string;
  readonly unitShortName: string;
  readonly warehouseName: string;
  readonly type: TransferType;
  readonly quantity: number;
  readonly date: Date | null;
  readonly note: string | null;
}

/** Zaxira harakatlari — tasdiqlangan transfer qatorlari (eski `warehouse/movements`). */
@Component({
  selector: 'app-movements',
  imports: [DecimalPipe, DatePipe, FormsModule, TranslocoDirective, TableModule, DatePicker, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './movements.component.html',
  styleUrl: './movements.component.scss',
})
export default class MovementsComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);

  protected readonly transferTypeClass = transferTypeClass;
  protected readonly transferTypeKey = transferTypeKey;

  readonly movements = signal<MovementRow[]>([]);
  readonly loading = signal(true);
  readonly dateFrom = signal<Date | null>(null);
  readonly dateTo = signal<Date | null>(null);

  ngOnInit(): void {
    this.loadMovements();
  }

  loadMovements(): void {
    this.loading.set(true);
    const from = this.dateFrom();
    const to = this.dateTo();
    this.warehouseService
      .getMovements({
        status: TransferStatus.Confirmed,
        pageSize: 500,
        // Backend `CreatedAt >= from` / `<= to` (UTC) qiladi — mahalliy kun chegaralari
        // UTC'ga o'giriladi. Eskisi `YYYY-MM-DD` yuborib, «gacha» kunining deyarli
        // hammasini (UTC yarim tundan keyingisini) tashlab yuborardi.
        from: from ? localDayRangeToUtc(from, from).from : undefined,
        to: to ? localDayRangeToUtc(to, to).to : undefined,
      })
      .subscribe({
        next: (res) => {
          this.movements.set(res.success && res.data ? flatten(res.data) : []);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onDateFrom(value: Date | null): void {
    this.dateFrom.set(value);
    this.loadMovements();
  }

  onDateTo(value: Date | null): void {
    this.dateTo.set(value);
    this.loadMovements();
  }
}

/** Har transferni mahsulot qatorlariga yoyadi. */
function flatten(transfers: readonly Transfer[]): MovementRow[] {
  return transfers.flatMap((tr) => {
    const warehouseName = warehouseFor(tr);
    const date = parseUtc(tr.confirmedAt ?? tr.createdAt);
    return tr.items.map((item) => ({
      productName: item.productName,
      unitShortName: item.unitShortName,
      warehouseName,
      type: tr.type,
      quantity: item.quantity,
      date,
      note: tr.note,
    }));
  });
}

function warehouseFor(tr: Transfer): string {
  switch (tr.type) {
    case TransferType.Outgoing:
      return tr.fromWarehouseName ?? '—';
    case TransferType.Internal:
      return `${tr.fromWarehouseName ?? '—'} → ${tr.toWarehouseName ?? '—'}`;
    default: // Kirim, qaytarish, ishlab chiqarish chiqimi — qabul qilgan ombor
      return tr.toWarehouseName ?? '—';
  }
}
