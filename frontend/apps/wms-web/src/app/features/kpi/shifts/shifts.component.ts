import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { Shift, ShiftCreateDto } from '../kpi.model';
import { KpiService } from '../kpi.service';

interface ShiftForm {
  name: string;
  startTime: string;
  endTime: string;
}

@Component({
  selector: 'app-shifts',
  imports: [FormsModule, TranslocoDirective, TableModule, Button, InputText, Dialog, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shifts.component.html',
  styleUrl: './shifts.component.scss',
})
export default class ShiftsComponent implements OnInit {
  private readonly kpiService = inject(KpiService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly shifts = signal<Shift[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  /** Tahrirlanayotgan smena id'si; `null` — yangi smena. */
  readonly editingId = signal<string | null>(null);

  readonly form = signal<ShiftForm>({ name: '', startTime: '', endTime: '' });

  ngOnInit(): void {
    this.loadShifts();
  }

  loadShifts(): void {
    this.loading.set(true);
    this.kpiService.getShifts().subscribe({
      next: (res) => {
        this.shifts.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set({ name: '', startTime: '', endTime: '' });
    this.editingId.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(shift: Shift): void {
    this.form.set({
      name: shift.name,
      startTime: shortTime(shift.startTime),
      endTime: shortTime(shift.endTime),
    });
    this.editingId.set(shift.id);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim() || !f.startTime.trim() || !f.endTime.trim()) {
      this.notify.warn(this.language.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    const dto: ShiftCreateDto = {
      name: f.name.trim(),
      startTime: toTimeSpan(f.startTime),
      endTime: toTimeSpan(f.endTime),
    };

    const id = this.editingId();
    const request = id === null ? this.kpiService.createShift(dto) : this.kpiService.updateShift(id, dto);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.loadShifts();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteShift(shift: Shift): void {
    this.notify.confirmDelete(`${shift.name}?`, () => {
      this.kpiService.deleteShift(shift.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('common.success'));
          this.loadShifts();
        },
      });
    });
  }

  updateForm<K extends keyof ShiftForm>(field: K, value: ShiftForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  readonly shortTime = shortTime;
}

/** `"08:00:00"` → `"08:00"` — soniya smena uchun ma'nosiz. */
function shortTime(value: string): string {
  return /^\d{1,2}:\d{2}:\d{2}/.test(value) ? value.slice(0, value.lastIndexOf(':')) : value;
}

/**
 * `"8:00"` → `"08:00:00"`. Backend maydoni .NET `TimeSpan`; System.Text.Json uni
 * `hh:mm:ss` shaklida kutadi, foydalanuvchi esa `HH:mm` yozadi.
 */
function toTimeSpan(value: string): string {
  const trimmed = value.trim();
  const match = /^(\d{1,2}):(\d{2})$/.exec(trimmed);
  return match ? `${match[1].padStart(2, '0')}:${match[2]}:00` : trimmed;
}
