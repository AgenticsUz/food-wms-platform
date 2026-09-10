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
import { Textarea } from 'primeng/textarea';

import { NotificationService } from '../../../core/notify/notification.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { ProductOption, Shift, ShiftActual } from '../kpi.model';
import { KpiService } from '../kpi.service';

interface ActualForm {
  shiftId: string | null;
  productId: string | null;
  actualQuantity: number | null;
  wasteQuantity: number | null;
  date: Date | null;
  note: string | null;
}

const EMPTY_FORM: ActualForm = {
  shiftId: null,
  productId: null,
  actualQuantity: null,
  wasteQuantity: null,
  date: null,
  note: null,
};

@Component({
  selector: 'app-actuals',
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
    Textarea,
    PageHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './actuals.component.html',
  styleUrl: './actuals.component.scss',
})
export default class ActualsComponent implements OnInit {
  private readonly kpiService = inject(KpiService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly actuals = signal<ShiftActual[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly products = signal<ProductOption[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);

  readonly form = signal<ActualForm>(EMPTY_FORM);

  ngOnInit(): void {
    this.loadActuals();
    this.kpiService.getShifts().subscribe((res) => {
      if (res.success && res.data) this.shifts.set(res.data);
    });
    this.kpiService.getProducts().subscribe((res) => {
      if (res.success && res.data) this.products.set(res.data);
    });
  }

  loadActuals(): void {
    this.loading.set(true);
    this.kpiService.getActuals().subscribe({
      next: (res) => {
        this.actuals.set(res.success ? (res.data ?? []) : []);
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
    const { shiftId, productId, actualQuantity, wasteQuantity, date, note } = this.form();
    if (!shiftId || !productId || !actualQuantity || !date) {
      this.notify.warn(this.language.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    this.kpiService
      .createActual({
        shiftId,
        productId,
        actualQuantity,
        wasteQuantity: wasteQuantity ?? 0,
        date: toLocalDateString(date),
        note: note?.trim() || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success(this.language.translate('common.success'));
          this.loadActuals();
        },
        error: () => this.saving.set(false),
      });
  }

  updateForm<K extends keyof ActualForm>(field: K, value: ActualForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
