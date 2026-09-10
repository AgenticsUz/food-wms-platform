import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { QcParameterType, type QcParameter, type QcParameterCreateDto } from '../settings.model';
import { SettingsService } from '../settings.service';

interface QcForm {
  name: string;
  unit: string | null;
  minValue: number | null;
  maxValue: number | null;
  valueType: QcParameterType;
}

const EMPTY_FORM: QcForm = {
  name: '',
  unit: null,
  minValue: null,
  maxValue: null,
  valueType: QcParameterType.Numeric,
};

/** Tur nomlari — texnik atamalar, eski ilovadagidek tarjima qilinmaydi. */
const TYPE_NAMES: Readonly<Record<number, string>> = {
  [QcParameterType.Numeric]: 'Numeric',
  [QcParameterType.Text]: 'Text',
  [QcParameterType.Boolean]: 'Boolean',
};

const TYPE_STATUS: Readonly<Record<number, string>> = {
  [QcParameterType.Numeric]: 'Active',
  [QcParameterType.Text]: 'InProgress',
  [QcParameterType.Boolean]: 'Pending',
};

@Component({
  selector: 'app-qc-parameters',
  imports: [
    FormsModule,
    DecimalPipe,
    TableModule,
    Button,
    InputText,
    InputNumber,
    Select,
    Dialog,
    PageHeaderComponent,
    StatusBadgeComponent,
    TranslocoDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './qc-parameters.component.html',
  styleUrl: './qc-parameters.component.scss',
})
export default class QcParametersComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly parameters = signal<QcParameter[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly form = signal<QcForm>(EMPTY_FORM);

  readonly typeOptions = [QcParameterType.Numeric, QcParameterType.Text, QcParameterType.Boolean].map(
    (value) => ({ label: TYPE_NAMES[value], value })
  );

  ngOnInit(): void {
    this.loadParameters();
  }

  loadParameters(): void {
    this.loading.set(true);
    this.settingsService.getQcParameters().subscribe({
      next: (res) => {
        this.parameters.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set(EMPTY_FORM);
    this.editingId.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(param: QcParameter): void {
    this.form.set({
      name: param.name,
      unit: param.unit,
      minValue: param.minValue,
      maxValue: param.maxValue,
      valueType: param.valueType,
    });
    this.editingId.set(param.id);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn(this.language.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    const dto: QcParameterCreateDto = {
      name: f.name.trim(),
      unit: f.unit?.trim() || null,
      minValue: f.minValue,
      maxValue: f.maxValue,
      valueType: f.valueType,
    };

    const id = this.editingId();
    const request =
      id === null ? this.settingsService.createQcParameter(dto) : this.settingsService.updateQcParameter(id, dto);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.loadParameters();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteParameter(param: QcParameter): void {
    this.notify.confirmDelete(`${param.name}?`, () => {
      this.settingsService.deleteQcParameter(param.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('common.success'));
          this.loadParameters();
        },
      });
    });
  }

  typeName(type: QcParameterType): string {
    return TYPE_NAMES[type] ?? '—';
  }

  typeStatus(type: QcParameterType): string {
    return TYPE_STATUS[type] ?? 'Draft';
  }

  updateForm<K extends keyof QcForm>(field: K, value: QcForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
