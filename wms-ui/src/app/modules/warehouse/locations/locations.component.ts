import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Location, LocationCreateDto, Warehouse } from '../../../core/models/warehouse.model';

@Component({
  selector: 'app-locations',
  standalone: true,
  imports: [FormsModule, TableModule, Button, InputText, Dialog, Select, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './locations.component.html',
  styleUrl: './locations.component.scss'
})
export default class LocationsComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  locations = signal<Location[]>([]);
  warehouses = signal<Warehouse[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<LocationCreateDto & { id?: number }>({ warehouseId: 0, name: '', code: null });

  ngOnInit() {
    this.loadWarehouses();
    this.loadLocations();
  }

  private loadWarehouses() {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => { if (res.success && res.data) this.warehouses.set(res.data); }
    });
  }

  loadLocations() {
    this.loading.set(true);
    this.warehouseService.getLocations().subscribe({
      next: (res) => {
        this.locations.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load locations'); }
    });
  }

  openNew() {
    this.form.set({ warehouseId: 0, name: '', code: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(loc: Location) {
    this.form.set({ id: loc.id, warehouseId: loc.warehouseId, name: loc.name, code: loc.code });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) { this.notify.warn('Location name is required'); return; }
    if (!f.warehouseId) { this.notify.warn('Select a warehouse'); return; }
    this.saving.set(true);
    const dto: LocationCreateDto = { warehouseId: f.warehouseId, name: f.name.trim(), code: f.code };
    const obs = this.editing()
      ? this.warehouseService.updateLocation(f.id!, dto)
      : this.warehouseService.createLocation(dto);
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible.set(false); this.notify.success(this.editing() ? 'Location updated' : 'Location created'); this.loadLocations(); },
      error: () => { this.saving.set(false); this.notify.error('Failed to save location'); }
    });
  }

  deleteLocation(loc: Location) {
    this.notify.confirmDelete(`Delete "${loc.name}"?`, () => {
      this.warehouseService.deleteLocation(loc.id).subscribe({
        next: () => { this.notify.success('Location deleted'); this.loadLocations(); },
        error: () => this.notify.error('Failed to delete location')
      });
    });
  }

  updateForm(field: string, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }
}
