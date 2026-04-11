import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { Checkbox } from 'primeng/checkbox';
import { Tooltip } from 'primeng/tooltip';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SettingsService } from '../../../core/services/settings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { RoleInfo, RoleCreateDto, PermissionInfo } from '../../../core/models/settings.model';

interface PermissionGroup {
  module: string;
  permissions: (PermissionInfo & { checked: boolean })[];
}

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [FormsModule, DatePipe, TableModule, Button, InputText, Dialog, Textarea, Checkbox, Tooltip, PageHeaderComponent, TranslocoDirective],
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

  // Permissions dialog
  permDialogVisible = signal(false);
  permLoading = signal(false);
  permSaving = signal(false);
  permRoleId = signal<number | null>(null);
  permRoleName = signal('');
  allPermissions = signal<PermissionInfo[]>([]);
  selectedPermissionIds = signal<number[]>([]);

  permissionGroups = computed<PermissionGroup[]>(() => {
    const perms = this.allPermissions();
    const selected = this.selectedPermissionIds();
    const groups = new Map<string, (PermissionInfo & { checked: boolean })[]>();

    for (const p of perms) {
      const module = p.module || p.code.split('.')[0].toUpperCase();
      if (!groups.has(module)) groups.set(module, []);
      groups.get(module)!.push({ ...p, checked: selected.includes(p.id) });
    }

    return Array.from(groups.entries()).map(([module, permissions]) => ({ module, permissions }));
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

  openPermissions(role: RoleInfo) {
    this.permRoleId.set(role.id);
    this.permRoleName.set(role.name);
    this.permLoading.set(true);
    this.permDialogVisible.set(true);

    // Load all permissions + role's current permissions
    this.settingsService.getPermissions().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.allPermissions.set(res.data);
        }
        this.settingsService.getRolePermissions(role.id).subscribe({
          next: (permRes) => {
            if (permRes.success && permRes.data) {
              this.selectedPermissionIds.set(permRes.data);
            }
            this.permLoading.set(false);
          },
          error: () => {
            this.permLoading.set(false);
            this.notify.error('Failed to load role permissions');
          }
        });
      },
      error: () => {
        this.permLoading.set(false);
        this.notify.error('Failed to load permissions');
      }
    });
  }

  togglePermission(permId: number, checked: boolean) {
    this.selectedPermissionIds.update(ids => {
      if (checked) {
        return [...ids, permId];
      } else {
        return ids.filter(id => id !== permId);
      }
    });
  }

  savePermissions() {
    const roleId = this.permRoleId();
    if (!roleId) return;

    this.permSaving.set(true);
    this.settingsService.updateRolePermissions(roleId, this.selectedPermissionIds()).subscribe({
      next: () => {
        this.permSaving.set(false);
        this.permDialogVisible.set(false);
        this.notify.success('Permissions updated');
      },
      error: () => {
        this.permSaving.set(false);
        this.notify.error('Failed to update permissions');
      }
    });
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
