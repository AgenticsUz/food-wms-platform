import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToggleSwitch } from 'primeng/toggleswitch';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Vehicle } from '../delivery.model';
import { DeliveryService } from '../delivery.service';

interface VehicleForm {
  name: string;
  model: string | null;
  capacity: number;
  isActive: boolean;
}

const EMPTY_FORM: VehicleForm = { name: '', model: null, capacity: 0, isActive: true };

@Component({
  selector: 'app-vehicles',
  imports: [
    DecimalPipe,
    FormsModule,
    TranslocoDirective,
    TableModule,
    Button,
    Dialog,
    InputText,
    InputNumber,
    ToggleSwitch,
    PageHeaderComponent,
    StatusBadgeComponent,
    HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vehicles.component.html',
  styleUrl: './vehicles.component.scss',
})
export default class VehiclesComponent implements OnInit {
  private readonly service = inject(DeliveryService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly vehicles = signal<Vehicle[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly dialogVisible = signal(false);
  /** Tahrirlanayotgan transport id'si; `null` — yangi. */
  readonly editingId = signal<string | null>(null);
  readonly form = signal<VehicleForm>(EMPTY_FORM);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getVehicles().subscribe({
      next: (res) => {
        this.vehicles.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editingId.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(v: Vehicle): void {
    this.form.set({ name: v.name, model: v.model, capacity: v.capacity, isActive: v.isActive });
    this.editingId.set(v.id);
    this.dialogVisible.set(true);
  }

  updateForm<K extends keyof VehicleForm>(field: K, value: VehicleForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn(this.language.translate('delivery.vehicleNameRequired'));
      return;
    }
    this.saving.set(true);
    const dto = { name: f.name.trim(), model: f.model, capacity: f.capacity, isActive: f.isActive };
    const id = this.editingId();
    const request = id === null ? this.service.createVehicle(dto) : this.service.updateVehicle(id, dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(v: Vehicle): void {
    this.notify.confirmDelete(`${v.name}?`, () => {
      this.service.deleteVehicle(v.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('common.success'));
          this.load();
        },
      });
    });
  }
}
