import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';

import { parseUtc } from '../../../core/utils/date.util';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import {
  transferStatusClass,
  transferStatusKey,
  transferTypeClass,
  transferTypeKey,
} from '../../transfers/transfer-enums';
import type { PortalTransfer } from '../portal.model';
import { PortalService } from '../portal.service';

/**
 * Kabinetdagi hujjatlar — «menga nima yuborilgan / men nima berganman».
 *
 * Qator yoyiladi: tovar, miqdor, narx, summa. Aynan shu ma'lumot uchun do'kon
 * egasi zavodga qo'ng'iroq qilardi (TG13 ning sababi bilan bir xil).
 */
@Component({
  selector: 'app-portal-transfers',
  imports: [DecimalPipe, DatePipe, TranslocoDirective, TableModule, Button, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-transfers.component.html',
  styleUrl: './portal-transfers.component.scss',
})
export default class PortalTransfersComponent implements OnInit {
  private readonly portal = inject(PortalService);

  protected readonly transferStatusClass = transferStatusClass;
  protected readonly transferStatusKey = transferStatusKey;
  protected readonly transferTypeClass = transferTypeClass;
  protected readonly transferTypeKey = transferTypeKey;

  readonly transfers = signal<PortalTransfer[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.portal.getTransfers(1, 100).subscribe({
      next: (res) => {
        this.transfers.set(res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  /**
   * Ro'yxatdagi sana — HUJJAT sanasi (P2.3), tasdiqlangan/yaratilgan lahza emas:
   * mijoz «tovar qaysi kuni keldi» ni so'raydi va server ham shu bo'yicha
   * tartiblaydi. Bu kun boshi, shuning uchun shablonda soat ko'rsatilmaydi.
   */
  moment(row: PortalTransfer): Date | null {
    return parseUtc(row.documentDate);
  }

  itemCount(row: PortalTransfer): number {
    return row.items?.length ?? 0;
  }
}
