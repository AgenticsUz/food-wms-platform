import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';

import { NotificationService } from '../../../core/notify/notification.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { ProductOption, Shift, ShiftPlan } from '../kpi.model';
import { KpiService } from '../kpi.service';

interface PlanForm {
  shiftId: string | null;
  productId: string | null;
  plannedQuantity: number | null;
  date: Date | null;
}

const EMPTY_FORM: PlanForm = { shiftId: null, productId: null, plannedQuantity: null, date: null };

@Component({
  selector: 'app-plans',
  imports: [
    FormsModule,
    TranslocoDirective,
    DecimalPipe,
    DatePipe,
    TableModule,
    Button,
    Dialog,
    Select,
    InputNumber,
    DatePicker,
    PageHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './plans.component.html',
  styleUrl: './plans.component.scss',
})
export default class PlansComponent implements OnInit {
  private readonly kpiService = inject(KpiService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly plans = signal<ShiftPlan[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly products = signal<ProductOption[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);

  readonly form = signal<PlanForm>(EMPTY_FORM);

  ngOnInit(): void {
    this.loadPlans();
    this.kpiService.getShifts().subscribe((res) => {
      if (res.success && res.data) this.shifts.set(res.data);
    });
    this.kpiService.getProducts().subscribe((res) => {
      if (res.success && res.data) this.products.set(res.data);
    });
  }

  loadPlans(): void {
    this.loading.set(true);
    this.kpiService.getPlans().subscribe({
      next: (res) => {
        this.plans.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.dialogVisible.set(true);
  }

  save(): void {
    const { shiftId, productId, plannedQuantity, date } = this.form();
    if (!shiftId || !productId || !plannedQuantity || !date) {
      this.notify.warn(this.language.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    // Reja kuni — FAQAT SANA: mahalliy kalendar kuni (`date.util` qoidasi).
    this.kpiService
      .createPlan({ shiftId, productId, plannedQuantity, date: toLocalDateString(date) })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success(this.language.translate('common.success'));
          this.loadPlans();
        },
        error: () => this.saving.set(false),
      });
  }

  updateForm<K extends keyof PlanForm>(field: K, value: PlanForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
