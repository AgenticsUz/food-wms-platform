import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { KpiService } from '../../../core/services/kpi.service';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Shift, ShiftActual, ShiftActualCreateDto } from '../../../core/models/kpi.model';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-actuals',
  standalone: true,
  imports: [
    FormsModule, DecimalPipe, DatePipe, TableModule, Button,
    Dialog, Select, InputNumber, DatePicker, Textarea, PageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './actuals.component.html',
  styleUrl: './actuals.component.scss'
})
export default class ActualsComponent implements OnInit {
  private kpiService = inject(KpiService);
  private productService = inject(ProductService);
  private notify = inject(NotificationService);

  actuals = signal<ShiftActual[]>([]);
  shifts = signal<Shift[]>([]);
  products = signal<Product[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  saving = signal(false);

  form = signal<{
    shiftId: number | null;
    productId: number | null;
    actualQuantity: number | null;
    wasteQuantity: number | null;
    date: Date | null;
    note: string | null;
  }>({
    shiftId: null,
    productId: null,
    actualQuantity: null,
    wasteQuantity: null,
    date: null,
    note: null
  });

  ngOnInit() {
    this.loadActuals();
    this.loadShifts();
    this.loadProducts();
  }

  loadActuals() {
    this.loading.set(true);
    this.kpiService.getActuals().subscribe({
      next: (res) => {
        this.actuals.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load actuals');
      }
    });
  }

  private loadShifts() {
    this.kpiService.getShifts().subscribe({
      next: (res) => {
        if (res.success && res.data) this.shifts.set(res.data);
      }
    });
  }

  private loadProducts() {
    this.productService.getProducts().subscribe({
      next: (res) => {
        if (res.success && res.data) this.products.set(res.data);
      }
    });
  }

  openNew() {
    this.form.set({
      shiftId: null,
      productId: null,
      actualQuantity: null,
      wasteQuantity: null,
      date: null,
      note: null
    });
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.shiftId || !f.productId || !f.actualQuantity || !f.date) {
      this.notify.warn('Shift, product, actual quantity, and date are required');
      return;
    }

    this.saving.set(true);
    const dto: ShiftActualCreateDto = {
      shiftId: f.shiftId,
      productId: f.productId,
      actualQuantity: f.actualQuantity,
      wasteQuantity: f.wasteQuantity ?? 0,
      date: f.date.toISOString(),
      note: f.note?.trim() || null
    };

    this.kpiService.createActual(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success('Actual recorded');
        this.loadActuals();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to record actual');
      }
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
