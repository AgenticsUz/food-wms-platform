import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { LanguageService } from '@agentics/i18n';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { injectTranslationTick } from '../translation-tick';
import { WarehouseType, type Warehouse } from '../warehouse.model';
import { WarehouseService } from '../warehouse.service';

interface WarehouseForm {
  readonly id: string | null;
  readonly name: string;
  readonly type: WarehouseType;
  readonly description: string | null;
}

const EMPTY_FORM: WarehouseForm = { id: null, name: '', type: WarehouseType.General, description: null };

/**
 * Omborlar ro'yxati (eski `warehouse/warehouse-list`).
 *
 * Eski dialogdagi `app-limit-notice` («ombor limitiga yaqinlashdingiz») yangi
 * qobiqda hali yo'q (`shared/components/limit-notice` ko'chirilmagan) —
 * shuning uchun olib tashlandi; limitga urilganda backend baribir 402 beradi.
 */
@Component({
  selector: 'app-warehouse-list',
  imports: [FormsModule, TranslocoDirective, TableModule, Button, InputText, Dialog, Select, Textarea, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './warehouse-list.component.html',
  styleUrl: './warehouse-list.component.scss',
})
export default class WarehouseListComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly translationTick = injectTranslationTick();

  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly form = signal<WarehouseForm>(EMPTY_FORM);

  readonly typeOptions = computed(() => {
    this.translationTick();
    return [WarehouseType.Raw, WarehouseType.Finished, WarehouseType.General].map((value) => ({
      label: this.language.translate(this.typeKey(value)),
      value,
    }));
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        this.warehouses.set(res.success && res.data ? res.data : []);
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

  openEdit(w: Warehouse): void {
    this.form.set({ id: w.id, name: w.name, type: w.type, description: w.description });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Name is required');
      return;
    }
    this.saving.set(true);
    const dto = { name: f.name.trim(), type: f.type, description: f.description };
    const request =
      f.id !== null ? this.warehouseService.updateWarehouse(f.id, dto) : this.warehouseService.createWarehouse(dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(f.id !== null ? 'Warehouse updated' : 'Warehouse created');
        this.loadData();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteWarehouse(w: Warehouse): void {
    this.notify.confirmDelete(`Delete "${w.name}"?`, () => {
      this.warehouseService.deleteWarehouse(w.id).subscribe({
        next: () => {
          this.notify.success('Warehouse deleted');
          this.loadData();
        },
        // Xato toastini `WmsErrorNotifier` allaqachon chiqardi (server sababi bilan).
        error: () => undefined,
      });
    });
  }

  typeKey(type: WarehouseType): string {
    switch (type) {
      case WarehouseType.Raw:
        return 'warehouse.typeRaw';
      case WarehouseType.Finished:
        return 'warehouse.typeFinished';
      default:
        return 'warehouse.typeGeneral';
    }
  }

  typeStatus(type: WarehouseType): string {
    switch (type) {
      case WarehouseType.Raw:
        return 'Pending';
      case WarehouseType.Finished:
        return 'Confirmed';
      case WarehouseType.General:
        return 'InProgress';
      default:
        return 'Active';
    }
  }

  updateForm<K extends keyof WarehouseForm>(field: K, value: WarehouseForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
