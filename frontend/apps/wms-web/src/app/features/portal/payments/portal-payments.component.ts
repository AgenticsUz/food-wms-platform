import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';

import { parseUtc } from '../../../core/utils/date.util';
import { paymentMethodKey } from '../../finance/finance.model';
import type { PortalPayment } from '../portal.model';
import { PortalService } from '../portal.service';

/** Kabinetdagi to'lovlar: kim kiritgani KO'RSATILMAYDI (zavodning ichki ma'lumoti). */
@Component({
  selector: 'app-portal-payments',
  imports: [DecimalPipe, DatePipe, TranslocoDirective, TableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-payments.component.html',
  styleUrl: './portal-payments.component.scss',
})
export default class PortalPaymentsComponent implements OnInit {
  private readonly portal = inject(PortalService);

  protected readonly methodKey = paymentMethodKey;

  readonly payments = signal<PortalPayment[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.portal.getPayments().subscribe({
      next: (res) => {
        this.payments.set(res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }
}
