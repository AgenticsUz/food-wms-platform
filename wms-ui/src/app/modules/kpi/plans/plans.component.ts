import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { KpiService } from '../../../core/services/kpi.service';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Shift, ShiftPlan, ShiftPlanCreateDto } from '../../../core/models/kpi.model';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-plans',
  standalone: true,
  imports: [
    FormsModule, TranslocoDirective, DecimalPipe, DatePipe, TableModule, Button,
    Dialog, Select, InputNumber, DatePicker, PageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './plans.component.html',
  styleUrl: './plans.component.scss'
})
export default class PlansComponent implements OnInit {
  private kpiService = inject(KpiService);
  private productService = inject(ProductService);
  private notify = inject(NotificationService);

  plans = signal<ShiftPlan[]>([]);
  shifts = signal<Shift[]>([]);
  products = signal<Product[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  saving = signal(false);

  form = signal<{ shiftId: number | null; productId: number | null; plannedQuantity: number | null; date: Date | null }>({
    shiftId: null,
    productId: null,
    plannedQuantity: null,
    date: null
  });

  ngOnInit() {
    this.loadPlans();
    this.loadShifts();
    this.loadProducts();
  }

  loadPlans() {
    this.loading.set(true);
    this.kpiService.getPlans().subscribe({
      next: (res) => {
        this.plans.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load plans');
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
    this.form.set({ shiftId: null, productId: null, plannedQuantity: null, date: null });
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.shiftId || !f.productId || !f.plannedQuantity || !f.date) {
      this.notify.warn('All fields are required');
      return;
    }

    this.saving.set(true);
    const dto: ShiftPlanCreateDto = {
      shiftId: f.shiftId,
      productId: f.productId,
      plannedQuantity: f.plannedQuantity,
      date: f.date.toISOString()
    };

    this.kpiService.createPlan(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success('Plan created');
        this.loadPlans();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to create plan');
      }
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
