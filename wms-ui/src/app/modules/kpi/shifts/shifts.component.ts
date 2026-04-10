import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { KpiService } from '../../../core/services/kpi.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Shift, ShiftCreateDto } from '../../../core/models/kpi.model';

@Component({
  selector: 'app-shifts',
  standalone: true,
  imports: [FormsModule, TableModule, Button, InputText, Dialog, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shifts.component.html',
  styleUrl: './shifts.component.scss'
})
export default class ShiftsComponent implements OnInit {
  private kpiService = inject(KpiService);
  private notify = inject(NotificationService);

  shifts = signal<Shift[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<ShiftCreateDto & { id?: number }>({
    name: '',
    startTime: '',
    endTime: ''
  });

  ngOnInit() {
    this.loadShifts();
  }

  loadShifts() {
    this.loading.set(true);
    this.kpiService.getShifts().subscribe({
      next: (res) => {
        this.shifts.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load shifts');
      }
    });
  }

  openNew() {
    this.form.set({ name: '', startTime: '', endTime: '' });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(shift: Shift) {
    this.form.set({
      id: shift.id,
      name: shift.name,
      startTime: shift.startTime,
      endTime: shift.endTime
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.startTime.trim() || !f.endTime.trim()) {
      this.notify.warn('All fields are required');
      return;
    }

    this.saving.set(true);
    const dto: ShiftCreateDto = {
      name: f.name.trim(),
      startTime: f.startTime.trim(),
      endTime: f.endTime.trim()
    };

    const obs = this.editing()
      ? this.kpiService.updateShift(f.id!, dto)
      : this.kpiService.createShift(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Shift updated' : 'Shift created');
        this.loadShifts();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to save shift');
      }
    });
  }

  deleteShift(shift: Shift) {
    this.notify.confirmDelete(`Delete "${shift.name}"?`, () => {
      this.kpiService.deleteShift(shift.id).subscribe({
        next: () => {
          this.notify.success('Shift deleted');
          this.loadShifts();
        },
        error: () => this.notify.error('Failed to delete shift')
      });
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
