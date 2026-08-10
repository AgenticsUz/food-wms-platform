import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Warehouse, WarehouseCreateDto, WarehouseType } from '../../../core/models/warehouse.model';
import { LimitNoticeComponent } from '../../../shared/components/limit-notice/limit-notice.component';

@Component({
  selector: 'app-warehouse-list',
  standalone: true,
  imports: [FormsModule, TranslocoDirective, TableModule, Button, InputText, Dialog, Select, Textarea, PageHeaderComponent, StatusBadgeComponent, LimitNoticeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './warehouse-list.component.html',
  styleUrl: './warehouse-list.component.scss'
})
export default class WarehouseListComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  warehouses = signal<Warehouse[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  typeOptions = [
    { label: 'Raw Materials', value: WarehouseType.Raw },
    { label: 'Finished Goods', value: WarehouseType.Finished },
    { label: 'General', value: WarehouseType.General }
  ];

  form = signal<WarehouseCreateDto & { id?: number }>({
    name: '', type: WarehouseType.General, description: null
  });

  ngOnInit() { this.loadData(); }

  loadData() {
    this.loading.set(true);
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        this.warehouses.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  openNew() {
    this.form.set({ name: '', type: WarehouseType.General, description: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(w: Warehouse) {
    this.form.set({ id: w.id, name: w.name, type: w.type, description: w.description });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) { this.notify.warn('Name is required'); return; }
    this.saving.set(true);
    const dto: WarehouseCreateDto = { name: f.name.trim(), type: f.type, description: f.description };
    const obs = this.editing()
      ? this.warehouseService.updateWarehouse(f.id!, dto)
      : this.warehouseService.createWarehouse(dto);
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible.set(false); this.notify.success(this.editing() ? 'Warehouse updated' : 'Warehouse created'); this.loadData(); },
      error: () => { this.saving.set(false); }
    });
  }

  deleteWarehouse(w: Warehouse) {
    this.notify.confirmDelete(`Delete "${w.name}"?`, () => {
      this.warehouseService.deleteWarehouse(w.id).subscribe({
        next: () => {
          this.notify.success('Warehouse deleted');
          this.loadData();
        },
        error: () => this.notify.error('Failed to delete warehouse')
      });
    });
  }

  getTypeName(type: WarehouseType): string {
    switch (type) {
      case WarehouseType.Raw: return 'Raw';
      case WarehouseType.Finished: return 'Finished';
      case WarehouseType.General: return 'General';
      default: return 'Unknown';
    }
  }

  getTypeStatus(type: WarehouseType): string {
    switch (type) {
      case WarehouseType.Raw: return 'Pending';
      case WarehouseType.Finished: return 'Confirmed';
      case WarehouseType.General: return 'InProgress';
      default: return 'Active';
    }
  }

  updateForm(field: string, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }
}
