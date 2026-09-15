import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { DatePicker } from 'primeng/datepicker';

import { parseUtc, toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { TransferStatus, TransferType, type Transfer } from '../../transfers/transfer.model';
import { transferTypeClass, transferTypeKey } from '../../transfers/transfer-enums';
import { WarehouseService } from '../warehouse.service';

interface MovementRow {
  /** Hujjatning qisqa raqami — qatorni hujjatga bog'laydi («#12»). */
  readonly number: number;
  /** Guid — tooltipda: operator qo'llab-quvvatlashga aynan shuni beradi. */
  readonly transferId: string;
  readonly productName: string;
  readonly unitShortName: string;
  /** Tovar kimdan keldi (kirim/qaytarishda kontragent, ichkida manba ombor). */
  readonly fromName: string;
  /** Tovar kimga ketdi (chiqimda kontragent, aks holda qabul qilgan ombor). */
  readonly toName: string;
  readonly type: TransferType;
  readonly quantity: number;
  readonly unitPrice: number;
  /** Qator summasi — `quantity * unitPrice` (javobdagi `totalPrice`). */
  readonly amount: number;
  /** HUJJAT sanasi (kalendar kuni) — tasdiqlangan lahza emas: hisobot shu bo'yicha. */
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

  /** Jadval ostidagi jami — filtr o'zgarganda o'zi qayta hisoblanadi. */
  readonly totalAmount = computed(() => this.movements().reduce((sum, row) => sum + row.amount, 0));
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
        // Backend endi HUJJAT SANASI bo'yicha filtrlaydi (`DocumentDate`, P2.3), u esa
        // kun boshi — shuning uchun kalendar kuni (`YYYY-MM-DD`) yuboriladi, vaqt
        // nuqtasi emas.
        from: from ? toLocalDateString(from) : undefined,
        to: to ? toLocalDateString(to) : undefined,
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
    // Sana — HUJJAT sanasi: filtr ham, hisobot ham shu ustunda; tasdiqlangan lahza
    // (`confirmedAt`) bilan ular boshqa kunga tushib ketardi.
    const date = parseUtc(tr.documentDate);
    const [fromName, toName] = partiesFor(tr);
    return tr.items.map((item) => ({
      number: tr.number,
      transferId: tr.id,
      productName: item.productName,
      unitShortName: item.unitShortName,
      fromName,
      toName,
      type: tr.type,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      amount: item.totalPrice,
      date,
      note: tr.note,
    }));
  });
}

/**
 * «Kimdan → kimga». Kontragent tomoni turga bog'liq: kirim va qaytarishda u
 * BERUVCHI, chiqimda OLUVCHI. Ichki o'tkazma va ishlab chiqarish chiqimida
 * kontragent yo'q — ikki tomon ham ombor (ishlab chiqarishda manba — sex).
 */
function partiesFor(tr: Transfer): readonly [string, string] {
  const dash = '—';
  const from = tr.fromWarehouseName ?? dash;
  const to = tr.toWarehouseName ?? dash;
  const party = tr.counterpartyName ?? dash;

  switch (tr.type) {
    case TransferType.Outgoing:
      return [from, party];
    case TransferType.Incoming:
    case TransferType.Return:
      return [party, to];
    default: // Ichki, ishlab chiqarish chiqimi
      return [from, to];
  }
}
