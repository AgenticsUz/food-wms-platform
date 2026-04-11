import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SettingsService } from '../../../core/services/settings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { RoleInfo, RoleCreateDto } from '../../../core/models/settings.model';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [FormsModule, DatePipe, TableModule, Button, InputText, Dialog, Textarea, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss'
})
export default class RolesComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private notify = inject(NotificationService);

  roles = signal<RoleInfo[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<RoleCreateDto & { id?: number }>({
    name: '',
    description: null
  });

  ngOnInit() {
    this.loadRoles();
  }

  loadRoles() {
    this.loading.set(true);
    this.settingsService.getRoles().subscribe({
      next: (res) => {
        this.roles.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load roles');
      }
    });
  }

  openNew() {
    this.form.set({ name: '', description: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(role: RoleInfo) {
    this.form.set({ id: role.id, name: role.name, description: role.description });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Role name is required');
      return;
    }

    this.saving.set(true);
    const dto: RoleCreateDto = {
      name: f.name.trim(),
      description: f.description?.trim() || null
    };

    const obs = this.editing()
      ? this.settingsService.updateRole(f.id!, dto)
      : this.settingsService.createRole(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Role updated' : 'Role created');
        this.loadRoles();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to save role');
      }
    });
  }

  deleteRole(role: RoleInfo) {
    this.notify.confirmDelete(`Delete "${role.name}"?`, () => {
      this.settingsService.deleteRole(role.id).subscribe({
        next: () => {
          this.notify.success('Role deleted');
          this.loadRoles();
        },
        error: () => this.notify.error('Failed to delete role')
      });
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
