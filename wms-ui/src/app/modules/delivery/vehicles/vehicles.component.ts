import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { NotificationService } from '../../../shared/services/notification.service';
import { DeliveryService } from '../../../core/services/delivery.service';
import { Vehicle } from '../../../core/models/delivery.model';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule, TranslocoDirective, TableModule, Button, Dialog,
    InputText, InputNumber, ToggleSwitch, PageHeaderComponent, StatusBadgeComponent, HasPermissionDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vehicles.component.html',
  styleUrl: './vehicles.component.scss'
})
export default class VehiclesComponent implements OnInit {
  private service = inject(DeliveryService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  vehicles = signal<Vehicle[]>([]);
  loading = signal(true);
  saving = signal(false);
  dialogVisible = signal(false);
  editing = signal(false);
  form = signal<{ id?: number; name: string; model: string | null; capacity: number; isActive: boolean }>(this.empty());

  private empty() { return { name: '', model: null as string | null, capacity: 0, isActive: true }; }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getVehicles().subscribe({
      next: (res) => { this.vehicles.set(res.success && res.data ? res.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openNew() { this.form.set(this.empty()); this.editing.set(false); this.dialogVisible.set(true); }
  openEdit(v: Vehicle) { this.form.set({ id: v.id, name: v.name, model: v.model, capacity: v.capacity, isActive: v.isActive }); this.editing.set(true); this.dialogVisible.set(true); }
  updateForm(field: string, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }

  save() {
    const f = this.form();
    if (!f.name.trim()) { this.notify.warn(this.transloco.translate('delivery.vehicleNameRequired')); return; }
    this.saving.set(true);
    const dto = { name: f.name.trim(), model: f.model, capacity: f.capacity, isActive: f.isActive };
    const obs = this.editing() ? this.service.updateVehicle(f.id!, dto) : this.service.createVehicle(dto);
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible.set(false); this.notify.success(this.transloco.translate('common.success')); this.load(); },
      error: () => this.saving.set(false)
    });
  }

  remove(v: Vehicle) {
    this.notify.confirmDelete(`${v.name}?`, () => {
      this.service.deleteVehicle(v.id).subscribe({ next: () => { this.notify.success(this.transloco.translate('common.success')); this.load(); } });
    });
  }
}
