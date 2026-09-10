import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { AttendanceMethod, type AttendanceLog, type Shift, type WorkerOption } from '../kpi.model';
import { KpiService } from '../kpi.service';

interface CheckInForm {
  userId: string | null;
  shiftId: string | null;
  method: AttendanceMethod | null;
}

const EMPTY_FORM: CheckInForm = { userId: null, shiftId: null, method: null };

/** Usul nomlari texnik atamalar (qurilma turi) — tarjima qilinmaydi. */
const METHOD_LABELS: Readonly<Record<number, string>> = {
  [AttendanceMethod.PIN]: 'PIN',
  [AttendanceMethod.FaceID]: 'FaceID',
  [AttendanceMethod.Manual]: 'Manual',
};

/** Qo'lda kiritilgan kirish — tekshirilmagan, shuning uchun «kutilmoqda» rangida. */
const METHOD_STATUS: Readonly<Record<number, string>> = {
  [AttendanceMethod.PIN]: 'Confirmed',
  [AttendanceMethod.FaceID]: 'Confirmed',
  [AttendanceMethod.Manual]: 'Pending',
};

@Component({
  selector: 'app-attendance',
  imports: [
    FormsModule,
    TranslocoDirective,
    DatePipe,
    TableModule,
    Button,
    Dialog,
    Select,
    PageHeaderComponent,
    StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './attendance.component.html',
  styleUrl: './attendance.component.scss',
})
export default class AttendanceComponent implements OnInit {
  private readonly kpiService = inject(KpiService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly logs = signal<AttendanceLog[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly workers = signal<WorkerOption[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);

  readonly form = signal<CheckInForm>(EMPTY_FORM);

  readonly methodOptions = [AttendanceMethod.PIN, AttendanceMethod.FaceID, AttendanceMethod.Manual].map(
    (value) => ({ label: METHOD_LABELS[value], value })
  );

  ngOnInit(): void {
    this.loadLogs();
    this.kpiService.getShifts().subscribe((res) => {
      if (res.success && res.data) this.shifts.set(res.data);
    });
    this.kpiService.getWorkers().subscribe({
      next: (res) => {
        if (res.success && res.data) this.workers.set(res.data);
      },
      // `settings.users` ruxsati bo'lmasa — ro'yxat bo'sh qoladi (servis izohi).
      error: () => this.workers.set([]),
    });
  }

  loadLogs(): void {
    this.loading.set(true);
    this.kpiService.getAttendance().subscribe({
      next: (res) => {
        this.logs.set(res.success ? (res.data ?? []) : []);
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
    const { userId, shiftId, method } = this.form();
    if (!userId || !shiftId || !method) {
      this.notify.warn(this.language.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    this.kpiService.checkIn({ userId, shiftId, method }).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.loadLogs();
      },
      error: () => this.saving.set(false),
    });
  }

  checkOut(log: AttendanceLog): void {
    const title = this.language.translate('kpi.checkOut');
    this.notify.confirmAction(`${log.userName} — ${title}?`, title, () => {
      this.kpiService.checkOut(log.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('common.success'));
          this.loadLogs();
        },
      });
    });
  }

  methodLabel(method: AttendanceMethod): string {
    return METHOD_LABELS[method] ?? '—';
  }

  methodStatus(method: AttendanceMethod): string {
    return METHOD_STATUS[method] ?? 'Pending';
  }

  updateForm<K extends keyof CheckInForm>(field: K, value: CheckInForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
