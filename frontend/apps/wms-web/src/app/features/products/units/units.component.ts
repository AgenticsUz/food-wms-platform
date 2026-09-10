import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { Unit } from '../product.model';
import { ProductService } from '../product.service';

interface UnitForm {
  readonly id: string | null;
  readonly name: string;
  readonly shortName: string;
}

const EMPTY_FORM: UnitForm = { id: null, name: '', shortName: '' };

/** O'lchov birliklari (eski `products/units`). */
@Component({
  selector: 'app-units',
  imports: [FormsModule, TableModule, Button, InputText, Dialog, TranslocoDirective, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './units.component.html',
  styleUrl: './units.component.scss',
})
export default class UnitsComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly notify = inject(NotificationService);

  readonly units = signal<Unit[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly form = signal<UnitForm>(EMPTY_FORM);

  ngOnInit(): void {
    this.loadUnits();
  }

  loadUnits(): void {
    this.loading.set(true);
    this.productService.getUnits().subscribe({
      next: (res) => {
        this.units.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      // Xato toastini `WmsErrorNotifier` allaqachon chiqardi — ikkinchisi qo'shilmaydi.
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(unit: Unit): void {
    this.form.set({ id: unit.id, name: unit.name, shortName: unit.shortName });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim() || !f.shortName.trim()) {
      this.notify.warn('Name and short name are required');
      return;
    }

    this.saving.set(true);
    const dto = { name: f.name.trim(), shortName: f.shortName.trim() };
    const request =
      f.id !== null ? this.productService.updateUnit(f.id, dto) : this.productService.createUnit(dto);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(f.id !== null ? 'Unit updated' : 'Unit created');
        this.loadUnits();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteUnit(unit: Unit): void {
    this.notify.confirmDelete(`Delete "${unit.name}"?`, () => {
      this.productService.deleteUnit(unit.id).subscribe({
        next: () => {
          this.notify.success('Unit deleted');
          this.loadUnits();
        },
        error: () => undefined,
      });
    });
  }

  updateForm<K extends keyof UnitForm>(field: K, value: UnitForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
