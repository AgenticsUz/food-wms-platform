import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { Dialog } from 'primeng/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { SettingsService } from '../../../core/services/settings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { QcParameter, QcParameterCreateDto, QcParameterType } from '../../../core/models/settings.model';

@Component({
  selector: 'app-qc-parameters',
  standalone: true,
  imports: [
    FormsModule, DecimalPipe, DatePipe, TableModule, Button, InputText,
    InputNumber, Select, Dialog, PageHeaderComponent, StatusBadgeComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './qc-parameters.component.html',
  styleUrl: './qc-parameters.component.scss'
})
export default class QcParametersComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private notify = inject(NotificationService);

  parameters = signal<QcParameter[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<QcParameterCreateDto & { id?: number }>({
    name: '',
    unit: null,
    minValue: null,
    maxValue: null,
    valueType: QcParameterType.Numeric
  });

  typeOptions = [
    { label: 'Numeric', value: QcParameterType.Numeric },
    { label: 'Text', value: QcParameterType.Text },
    { label: 'Boolean', value: QcParameterType.Boolean }
  ];

  ngOnInit() {
    this.loadParameters();
  }

  loadParameters() {
    this.loading.set(true);
    this.settingsService.getQcParameters().subscribe({
      next: (res) => {
        this.parameters.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load QC parameters');
      }
    });
  }

  openNew() {
    this.form.set({
      name: '',
      unit: null,
      minValue: null,
      maxValue: null,
      valueType: QcParameterType.Numeric
    });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(param: QcParameter) {
    this.form.set({
      id: param.id,
      name: param.name,
      unit: param.unit,
      minValue: param.minValue,
      maxValue: param.maxValue,
      valueType: param.valueType
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Parameter name is required');
      return;
    }

    this.saving.set(true);
    const dto: QcParameterCreateDto = {
      name: f.name.trim(),
      unit: f.unit?.trim() || null,
      minValue: f.minValue,
      maxValue: f.maxValue,
      valueType: f.valueType
    };

    const obs = this.editing()
      ? this.settingsService.updateQcParameter(f.id!, dto)
      : this.settingsService.createQcParameter(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Parameter updated' : 'Parameter created');
        this.loadParameters();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to save parameter');
      }
    });
  }

  deleteParameter(param: QcParameter) {
    this.notify.confirmDelete(`Delete "${param.name}"?`, () => {
      this.settingsService.deleteQcParameter(param.id).subscribe({
        next: () => {
          this.notify.success('Parameter deleted');
          this.loadParameters();
        },
        error: () => this.notify.error('Failed to delete parameter')
      });
    });
  }

  getTypeName(type: QcParameterType): string {
    switch (type) {
      case QcParameterType.Numeric: return 'Numeric';
      case QcParameterType.Text: return 'Text';
      case QcParameterType.Boolean: return 'Boolean';
      default: return 'Unknown';
    }
  }

  getTypeStatus(type: QcParameterType): string {
    switch (type) {
      case QcParameterType.Numeric: return 'Active';
      case QcParameterType.Text: return 'InProgress';
      case QcParameterType.Boolean: return 'Pending';
      default: return 'Draft';
    }
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
