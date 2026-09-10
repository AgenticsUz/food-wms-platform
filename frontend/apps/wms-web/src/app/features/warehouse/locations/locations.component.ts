import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { Location, Warehouse } from '../warehouse.model';
import { WarehouseService } from '../warehouse.service';

interface LocationForm {
  readonly id: string | null;
  readonly warehouseId: string | null;
  readonly name: string;
  readonly code: string | null;
}

const EMPTY_FORM: LocationForm = { id: null, warehouseId: null, name: '', code: null };

/** Ombor joylari — javon/tokchalar (eski `warehouse/locations`). */
@Component({
  selector: 'app-locations',
  imports: [FormsModule, TranslocoDirective, TableModule, Button, InputText, Dialog, Select, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './locations.component.html',
  styleUrl: './locations.component.scss',
})
export default class LocationsComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly notify = inject(NotificationService);

  readonly locations = signal<Location[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly form = signal<LocationForm>(EMPTY_FORM);

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadLocations();
  }

  private loadWarehouses(): void {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        if (res.success && res.data) this.warehouses.set(res.data);
      },
    });
  }

  loadLocations(): void {
    this.loading.set(true);
    this.warehouseService.getLocations().subscribe({
      next: (res) => {
        this.locations.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(loc: Location): void {
    this.form.set({ id: loc.id, warehouseId: loc.warehouseId, name: loc.name, code: loc.code });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Location name is required');
      return;
    }
    if (f.warehouseId === null) {
      this.notify.warn('Select a warehouse');
      return;
    }
    this.saving.set(true);
    const dto = { warehouseId: f.warehouseId, name: f.name.trim(), code: f.code };
    const request =
      f.id !== null ? this.warehouseService.updateLocation(f.id, dto) : this.warehouseService.createLocation(dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(f.id !== null ? 'Location updated' : 'Location created');
        this.loadLocations();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteLocation(loc: Location): void {
    this.notify.confirmDelete(`Delete "${loc.name}"?`, () => {
      this.warehouseService.deleteLocation(loc.id).subscribe({
        next: () => {
          this.notify.success('Location deleted');
          this.loadLocations();
        },
        // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
        error: () => undefined,
      });
    });
  }

  updateForm<K extends keyof LocationForm>(field: K, value: LocationForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
