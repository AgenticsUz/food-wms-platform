import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { KpiService } from '../../../core/services/kpi.service';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Shift, AttendanceLog, AttendanceMethod, CheckInDto } from '../../../core/models/kpi.model';

interface SimpleUser {
  id: number;
  fullName: string;
}

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [
    FormsModule, DatePipe, TableModule, Button, Dialog, Select,
    PageHeaderComponent, StatusBadgeComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './attendance.component.html',
  styleUrl: './attendance.component.scss'
})
export default class AttendanceComponent implements OnInit {
  private kpiService = inject(KpiService);
  private apiService = inject(ApiService);
  private notify = inject(NotificationService);

  logs = signal<AttendanceLog[]>([]);
  shifts = signal<Shift[]>([]);
  users = signal<SimpleUser[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  saving = signal(false);

  form = signal<{ userId: number | null; shiftId: number | null; method: AttendanceMethod | null }>({
    userId: null,
    shiftId: null,
    method: null
  });

  methodOptions = [
    { label: 'PIN', value: AttendanceMethod.PIN },
    { label: 'FaceID', value: AttendanceMethod.FaceID },
    { label: 'Manual', value: AttendanceMethod.Manual }
  ];

  ngOnInit() {
    this.loadLogs();
    this.loadShifts();
    this.loadUsers();
  }

  loadLogs() {
    this.loading.set(true);
    this.kpiService.getAttendance().subscribe({
      next: (res) => {
        this.logs.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load attendance logs');
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

  private loadUsers() {
    this.apiService.get<SimpleUser[]>('users').subscribe({
      next: (res) => {
        if (res.success && res.data) this.users.set(res.data);
      }
    });
  }

  openNew() {
    this.form.set({ userId: null, shiftId: null, method: null });
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.userId || !f.shiftId || !f.method) {
      this.notify.warn('All fields are required');
      return;
    }

    this.saving.set(true);
    const dto: CheckInDto = {
      userId: f.userId,
      shiftId: f.shiftId,
      method: f.method
    };

    this.kpiService.checkIn(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success('Check-in recorded');
        this.loadLogs();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to check in');
      }
    });
  }

  checkOut(log: AttendanceLog) {
    this.notify.confirmAction('Confirm check-out for this worker?', 'Check Out', () => {
      this.kpiService.checkOut(log.id).subscribe({
        next: () => {
          this.notify.success('Checked out successfully');
          this.loadLogs();
        },
        error: () => this.notify.error('Failed to check out')
      });
    });
  }

  getMethodLabel(method: AttendanceMethod): string {
    const map: Record<number, string> = {
      [AttendanceMethod.PIN]: 'PIN',
      [AttendanceMethod.FaceID]: 'FaceID',
      [AttendanceMethod.Manual]: 'Manual'
    };
    return map[method] ?? 'Unknown';
  }

  getMethodStatus(method: AttendanceMethod): string {
    const map: Record<number, string> = {
      [AttendanceMethod.PIN]: 'Confirmed',
      [AttendanceMethod.FaceID]: 'Confirmed',
      [AttendanceMethod.Manual]: 'Pending'
    };
    return map[method] ?? 'Pending';
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
