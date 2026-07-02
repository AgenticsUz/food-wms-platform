import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { NotificationService } from '../../../shared/services/notification.service';
import { DeliveryService } from '../../../core/services/delivery.service';
import { CounterpartyService } from '../../../core/services/counterparty.service';
import { Vehicle, Driver, CreateDeliveryStopDto } from '../../../core/models/delivery.model';
import { Counterparty } from '../../../core/models/counterparty.model';
import { toLocalDateString } from '../../../shared/utils/date.util';

interface StopRow { counterpartyId: number | null; address: string | null; }

@Component({
  selector: 'app-delivery-create',
  standalone: true,
  imports: [FormsModule, TranslocoDirective, Button, Select, DatePicker, InputText, Textarea, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './delivery-create.component.html',
  styleUrl: './delivery-create.component.scss'
})
export default class DeliveryCreateComponent implements OnInit {
  private service = inject(DeliveryService);
  private counterpartySvc = inject(CounterpartyService);
  private notify = inject(NotificationService);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  vehicles = signal<Vehicle[]>([]);
  drivers = signal<Driver[]>([]);
  clients = signal<Counterparty[]>([]);

  vehicleId = signal<number | null>(null);
  driverId = signal<number | null>(null);
  scheduledDate = signal<Date>(new Date());
  note = signal('');
  stops = signal<StopRow[]>([{ counterpartyId: null, address: null }]);
  saving = signal(false);

  ngOnInit() {
    this.service.getVehicles().subscribe(res => { if (res.success && res.data) this.vehicles.set(res.data.filter(v => v.isActive)); });
    this.service.getDrivers().subscribe(res => { if (res.success && res.data) this.drivers.set(res.data.filter(d => d.isActive)); });
    // Clients + Both (type 2 yoki 3)
    this.counterpartySvc.getCounterparties().subscribe(res => {
      if (res.success && res.data) this.clients.set(res.data.filter(c => c.type === 2 || c.type === 3));
    });
  }

  addStop() { this.stops.update(s => [...s, { counterpartyId: null, address: null }]); }
  removeStop(i: number) { this.stops.update(s => s.filter((_, idx) => idx !== i)); }
  updateStop(i: number, field: keyof StopRow, value: unknown) {
    this.stops.update(s => s.map((st, idx) => idx === i ? { ...st, [field]: value } : st));
  }

  submit() {
    const validStops = this.stops().filter(s => s.counterpartyId);
    if (validStops.length === 0) { this.notify.warn(this.transloco.translate('delivery.atLeastOneStop')); return; }

    this.saving.set(true);
    const stopsDto: CreateDeliveryStopDto[] = validStops.map((s, i) => ({
      counterpartyId: s.counterpartyId!, transferId: null, address: s.address, sequenceOrder: i + 1, note: null
    }));
    this.service.createDelivery({
      vehicleId: this.vehicleId(),
      driverId: this.driverId(),
      scheduledDate: toLocalDateString(this.scheduledDate()),
      note: this.note() || null,
      stops: stopsDto
    }).subscribe({
      next: (res) => { this.saving.set(false); this.router.navigate(['/delivery', res.data?.id ?? '']); },
      error: () => this.saving.set(false)
    });
  }

  goBack() { this.router.navigate(['/delivery']); }
}
