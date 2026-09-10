import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';

import { NotificationService } from '../../../core/notify/notification.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import {
  COUNTERPARTY_BOTH,
  COUNTERPARTY_CLIENT,
  type ClientOption,
  type CreateDeliveryStopDto,
  type Driver,
  type Vehicle,
} from '../delivery.model';
import { DeliveryService } from '../delivery.service';

interface StopRow {
  counterpartyId: string | null;
  address: string | null;
}

@Component({
  selector: 'app-delivery-create',
  imports: [FormsModule, TranslocoDirective, Button, Select, DatePicker, InputText, Textarea, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './delivery-create.component.html',
  styleUrl: './delivery-create.component.scss',
})
export default class DeliveryCreateComponent implements OnInit {
  private readonly service = inject(DeliveryService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);

  readonly vehicles = signal<Vehicle[]>([]);
  readonly drivers = signal<Driver[]>([]);
  readonly clients = signal<ClientOption[]>([]);

  readonly vehicleId = signal<string | null>(null);
  readonly driverId = signal<string | null>(null);
  readonly scheduledDate = signal<Date>(new Date());
  readonly note = signal('');
  readonly stops = signal<StopRow[]>([{ counterpartyId: null, address: null }]);
  readonly saving = signal(false);

  ngOnInit(): void {
    this.service.getVehicles().subscribe((res) => {
      if (res.success && res.data) this.vehicles.set(res.data.filter((v) => v.isActive));
    });
    this.service.getDrivers().subscribe((res) => {
      if (res.success && res.data) this.drivers.set(res.data.filter((d) => d.isActive));
    });
    // Manzil — faqat mijoz yoki «ikkalasi» turidagi hamkor (ta'minotchiga yetkazilmaydi).
    this.service.getCounterparties().subscribe((res) => {
      if (res.success && res.data) {
        this.clients.set(
          res.data.filter((c) => c.type === COUNTERPARTY_CLIENT || c.type === COUNTERPARTY_BOTH)
        );
      }
    });
  }

  addStop(): void {
    this.stops.update((s) => [...s, { counterpartyId: null, address: null }]);
  }

  removeStop(i: number): void {
    this.stops.update((s) => s.filter((_, idx) => idx !== i));
  }

  updateStop<K extends keyof StopRow>(i: number, field: K, value: StopRow[K]): void {
    this.stops.update((s) => s.map((st, idx) => (idx === i ? { ...st, [field]: value } : st)));
  }

  submit(): void {
    const stops: CreateDeliveryStopDto[] = [];
    for (const s of this.stops()) {
      if (s.counterpartyId) {
        stops.push({
          counterpartyId: s.counterpartyId,
          transferId: null,
          address: s.address,
          sequenceOrder: stops.length + 1,
          note: null,
        });
      }
    }
    if (stops.length === 0) {
      this.notify.warn(this.language.translate('delivery.atLeastOneStop'));
      return;
    }

    this.saving.set(true);
    this.service
      .createDelivery({
        vehicleId: this.vehicleId(),
        driverId: this.driverId(),
        // Rejalashtirilgan kun — FAQAT SANA (mahalliy kalendar kuni).
        scheduledDate: toLocalDateString(this.scheduledDate()),
        note: this.note() || null,
        stops,
      })
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          const id = res.data?.id;
          void this.router.navigate(id ? ['/delivery', id] : ['/delivery']);
        },
        error: () => this.saving.set(false),
      });
  }

  goBack(): void {
    void this.router.navigate(['/delivery']);
  }
}
