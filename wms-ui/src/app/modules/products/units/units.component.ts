import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Unit, UnitCreateDto } from '../../../core/models/product.model';

@Component({
  selector: 'app-units',
  standalone: true,
  imports: [FormsModule, TableModule, Button, InputText, Dialog, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './units.component.html',
  styleUrl: './units.component.scss'
})
export default class UnitsComponent implements OnInit {
  private productService = inject(ProductService);
  private notify = inject(NotificationService);

  units = signal<Unit[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<UnitCreateDto & { id?: number }>({
    name: '',
    shortName: ''
  });

  ngOnInit() {
    this.loadUnits();
  }

  loadUnits() {
    this.loading.set(true);
    this.productService.getUnits().subscribe({
      next: (res) => {
        this.units.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load units');
      }
    });
  }

  openNew() {
    this.form.set({ name: '', shortName: '' });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(unit: Unit) {
    this.form.set({ id: unit.id, name: unit.name, shortName: unit.shortName });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.shortName.trim()) {
      this.notify.warn('Name and short name are required');
      return;
    }

    this.saving.set(true);
    const dto: UnitCreateDto = { name: f.name.trim(), shortName: f.shortName.trim() };

    const obs = this.editing()
      ? this.productService.updateUnit(f.id!, dto)
      : this.productService.createUnit(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Unit updated' : 'Unit created');
        this.loadUnits();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to save unit');
      }
    });
  }

  deleteUnit(unit: Unit) {
    this.notify.confirmDelete(`Delete "${unit.name}"?`, () => {
      this.productService.deleteUnit(unit.id).subscribe({
        next: () => {
          this.notify.success('Unit deleted');
          this.loadUnits();
        },
        error: () => this.notify.error('Failed to delete unit')
      });
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
